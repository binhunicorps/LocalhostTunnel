using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Core.Configuration;
using LocalhostTunnel.Core.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LocalhostTunnel.Infrastructure.Forwarding;

public sealed class ForwarderHost : IForwarderHost
{
    private readonly object _sync = new();

    private WebApplication? _webApplication;
    private RequestForwarder? _requestForwarder;
    private ForwarderSnapshot _current = new();
    private DateTimeOffset? _startedAt;

    public ForwarderSnapshot Current
    {
        get
        {
            lock (_sync)
            {
                if (_current.State == ServiceState.Running && _startedAt.HasValue)
                {
                    return ForwarderSnapshot.CreateRunning(_startedAt.Value, Environment.ProcessId);
                }

                return _current;
            }
        }
    }

    public async Task StartAsync(AppConfig config, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);

        WebApplication app;
        RequestForwarder requestForwarder;

        lock (_sync)
        {
            if (_webApplication is not null)
            {
                throw new InvalidOperationException("Forwarder host is already running.");
            }

            _current = new ForwarderSnapshot
            {
                State = ServiceState.Starting
            };
        }

        try
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri($"{config.TargetProtocol}://{config.TargetHost}:{config.TargetPort}"),
                Timeout = TimeSpan.FromMilliseconds(config.UpstreamTimeout)
            };

            requestForwarder = new RequestForwarder(httpClient, new LocalUrlRewriter(), config);

            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseUrls(BuildListenUrl(config));

            app = builder.Build();
            app.Map("/{**path}", context => HandleRequestAsync(context, config, requestForwarder));

            await app.StartAsync(cancellationToken);
        }
        catch
        {
            lock (_sync)
            {
                _current = new ForwarderSnapshot
                {
                    State = ServiceState.Faulted
                };
                _startedAt = null;
            }

            throw;
        }

        lock (_sync)
        {
            _startedAt = DateTimeOffset.UtcNow;
            _webApplication = app;
            _requestForwarder = requestForwarder;
            _current = ForwarderSnapshot.CreateRunning(_startedAt.Value, Environment.ProcessId);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        WebApplication? appToStop;
        RequestForwarder? requestForwarderToDispose;

        lock (_sync)
        {
            appToStop = _webApplication;
            requestForwarderToDispose = _requestForwarder;

            if (appToStop is null)
            {
                _current = new ForwarderSnapshot();
                _startedAt = null;
                return;
            }

            _current = new ForwarderSnapshot
            {
                State = ServiceState.Stopping,
                StartedAt = _startedAt
            };

            _webApplication = null;
            _requestForwarder = null;
        }

        await appToStop.StopAsync(cancellationToken);
        await appToStop.DisposeAsync();
        requestForwarderToDispose?.Dispose();

        lock (_sync)
        {
            _current = new ForwarderSnapshot
            {
                State = ServiceState.Stopped
            };
            _startedAt = null;
        }
    }

    private async Task HandleRequestAsync(
        HttpContext context,
        AppConfig config,
        RequestForwarder requestForwarder)
    {
        var requestId = Guid.NewGuid().ToString("N");

        if (IsHealthRequest(context.Request.Path))
        {
            await HandleHealthAsync(context, requestId);
            return;
        }

        if (!VerifyWebhookSecret(context.Request.Headers, config.WebhookSecret))
        {
            await WriteJsonErrorAsync(context, StatusCodes.Status401Unauthorized, "Unauthorized", requestId);
            return;
        }

        byte[] body;
        try
        {
            body = await ReadRequestBodyAsync(context.Request, config.MaxBodySize, context.RequestAborted);
        }
        catch (BodyTooLargeException)
        {
            await WriteJsonErrorAsync(context, StatusCodes.Status413PayloadTooLarge, "Payload Too Large", requestId);
            return;
        }

        try
        {
            var result = await requestForwarder.ForwardAsync(
                context.Request.Method,
                $"{context.Request.Path}{context.Request.QueryString}",
                context.Request.Headers,
                body,
                requestId,
                context.RequestAborted);

            context.Response.StatusCode = result.StatusCode;
            CopyResponseHeaders(context.Response, result.Headers, requestId);
            await context.Response.Body.WriteAsync(result.Body, context.RequestAborted);
        }
        catch (UpstreamTimeoutException)
        {
            await WriteJsonErrorAsync(context, StatusCodes.Status504GatewayTimeout, "Gateway Timeout", requestId);
        }
        catch (UpstreamUnavailableException)
        {
            await WriteJsonErrorAsync(context, StatusCodes.Status502BadGateway, "Bad Gateway", requestId);
        }
    }

    private async Task HandleHealthAsync(HttpContext context, string requestId)
    {
        var uptime = TimeSpan.Zero;

        lock (_sync)
        {
            if (_startedAt.HasValue)
            {
                uptime = DateTimeOffset.UtcNow - _startedAt.Value;
            }
        }

        var payload = JsonSerializer.Serialize(new
        {
            status = "ok",
            uptime = uptime.TotalSeconds,
            timestamp = DateTimeOffset.UtcNow
        });

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json";
        context.Response.Headers["x-request-id"] = requestId;
        await context.Response.WriteAsync(payload, context.RequestAborted);
    }

    private static bool IsHealthRequest(PathString path)
    {
        return path.Equals("/health", StringComparison.OrdinalIgnoreCase) ||
               path.Equals("/health/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool VerifyWebhookSecret(IHeaderDictionary headers, string webhookSecret)
    {
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            return true;
        }

        if (!headers.TryGetValue("x-webhook-secret", out var providedValues))
        {
            return false;
        }

        var provided = providedValues.ToString();
        if (provided.Length != webhookSecret.Length)
        {
            return false;
        }

        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(webhookSecret);
        return CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }

    private static void CopyResponseHeaders(HttpResponse response, IReadOnlyDictionary<string, string[]> headers, string requestId)
    {
        foreach (var pair in headers)
        {
            response.Headers[pair.Key] = pair.Value;
        }

        response.Headers["x-request-id"] = requestId;
    }

    private static async Task WriteJsonErrorAsync(HttpContext context, int statusCode, string message, string requestId)
    {
        var payload = JsonSerializer.Serialize(new
        {
            error = message,
            requestId
        });

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        context.Response.Headers["x-request-id"] = requestId;
        await context.Response.WriteAsync(payload, context.RequestAborted);
    }

    private static async Task<byte[]> ReadRequestBodyAsync(HttpRequest request, int maxBodySize, CancellationToken cancellationToken)
    {
        if (maxBodySize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxBodySize), "Maximum body size must be greater than zero.");
        }

        if (request.ContentLength.HasValue && request.ContentLength.Value > maxBodySize)
        {
            throw new BodyTooLargeException();
        }

        using var content = new MemoryStream();
        var buffer = new byte[8192];
        var totalRead = 0;

        while (true)
        {
            var read = await request.Body.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
            if (totalRead > maxBodySize)
            {
                throw new BodyTooLargeException();
            }

            await content.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return content.ToArray();
    }

    private static string BuildListenUrl(AppConfig config)
    {
        var host = string.IsNullOrWhiteSpace(config.Host) ? "127.0.0.1" : config.Host;
        return $"http://{host}:{config.Port}";
    }
}

internal sealed class BodyTooLargeException : Exception
{
}
