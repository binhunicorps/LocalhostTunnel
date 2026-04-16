using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Core.Logging;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Text;

namespace LocalhostTunnel.Infrastructure.Tavily;

public sealed class TavilyProxyService
{
    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "connection",
        "keep-alive",
        "proxy-authenticate",
        "proxy-authorization",
        "te",
        "trailer",
        "transfer-encoding",
        "upgrade",
        "host",
        "content-length"
    };

    private readonly Uri _baseUri;
    private readonly TavilyKeyManager _keyManager;
    private readonly HttpClient _httpClient;
    private readonly ILogStore _logStore;
    private readonly string _profileId;

    public TavilyProxyService(
        string baseUrl,
        TavilyKeyManager keyManager,
        HttpClient httpClient,
        ILogStore logStore,
        string profileId)
    {
        _baseUri = new Uri(baseUrl, UriKind.Absolute);
        _keyManager = keyManager;
        _httpClient = httpClient;
        _logStore = logStore;
        _profileId = profileId;
    }

    public async Task HandleAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var requestBody = await ReadBodyAsync(context.Request, cancellationToken);
        var (primaryKey, fallbackKey) = _keyManager.GetKeyOrder();

        using var firstResponse = await SendAsync(context.Request, requestBody, primaryKey, cancellationToken);
        if (IsStreamingSuccess(firstResponse))
        {
            await WriteStreamResponseAsync(context, firstResponse, cancellationToken);
            return;
        }

        var firstBody = await firstResponse.Content.ReadAsByteArrayAsync(cancellationToken);
        var firstText = Encoding.UTF8.GetString(firstBody);
        if (!TavilyQuotaDetector.IsQuotaExhausted((int)firstResponse.StatusCode, firstText))
        {
            await WriteBufferedResponseAsync(context, firstResponse, firstBody, cancellationToken);
            return;
        }

        _logStore.Append(LogEntry.Warn("tavily", "primary key quota exhausted; retrying with fallback key", _profileId));

        using var secondResponse = await SendAsync(context.Request, requestBody, fallbackKey, cancellationToken);
        if (IsStreamingSuccess(secondResponse))
        {
            _keyManager.Promote(fallbackKey);
            await WriteStreamResponseAsync(context, secondResponse, cancellationToken);
            return;
        }

        var secondBody = await secondResponse.Content.ReadAsByteArrayAsync(cancellationToken);
        if ((int)secondResponse.StatusCode is >= 200 and < 400)
        {
            _keyManager.Promote(fallbackKey);
        }

        await WriteBufferedResponseAsync(context, secondResponse, secondBody, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpRequest request,
        byte[] body,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var path = request.Path.Value ?? string.Empty;
        var query = request.QueryString.Value ?? string.Empty;
        var relative = $"{path}{query}".TrimStart('/');
        var targetUri = new Uri(_baseUri, relative);

        var proxyRequest = new HttpRequestMessage(new HttpMethod(request.Method), targetUri);
        CopyRequestHeaders(request, proxyRequest, apiKey);

        if (body.Length > 0)
        {
            var content = new ByteArrayContent(body);
            if (!string.IsNullOrWhiteSpace(request.ContentType))
            {
                content.Headers.ContentType = MediaTypeHeaderValue.Parse(request.ContentType);
            }

            proxyRequest.Content = content;
        }

        try
        {
            return await _httpClient.SendAsync(proxyRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logStore.Append(LogEntry.Error("tavily", $"upstream request failed: {ex.Message}", _profileId));
            throw new InvalidOperationException("Failed to reach Tavily upstream.", ex);
        }
    }

    private static void CopyRequestHeaders(HttpRequest request, HttpRequestMessage proxyRequest, string apiKey)
    {
        foreach (var header in request.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key) || string.Equals(header.Key, "authorization", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var values = header.Value.ToArray();
            if (!proxyRequest.Headers.TryAddWithoutValidation(header.Key, values))
            {
                proxyRequest.Content ??= new ByteArrayContent(Array.Empty<byte>());
                proxyRequest.Content.Headers.TryAddWithoutValidation(header.Key, values);
            }
        }

        proxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    private static bool IsStreamingSuccess(HttpResponseMessage response)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType;
        return (int)response.StatusCode < 400 &&
               string.Equals(contentType, "text/event-stream", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WriteStreamResponseAsync(HttpContext context, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = (int)response.StatusCode;
        CopyResponseHeaders(context.Response, response);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await stream.CopyToAsync(context.Response.Body, cancellationToken);
    }

    private static async Task WriteBufferedResponseAsync(HttpContext context, HttpResponseMessage response, byte[] body, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = (int)response.StatusCode;
        CopyResponseHeaders(context.Response, response);
        await context.Response.Body.WriteAsync(body, cancellationToken);
    }

    private static void CopyResponseHeaders(HttpResponse response, HttpResponseMessage upstream)
    {
        foreach (var header in upstream.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key))
            {
                continue;
            }

            response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in upstream.Content.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key))
            {
                continue;
            }

            response.Headers[header.Key] = header.Value.ToArray();
        }
    }

    private static async Task<byte[]> ReadBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentLength is null or 0)
        {
            return Array.Empty<byte>();
        }

        await using var ms = new MemoryStream();
        await request.Body.CopyToAsync(ms, cancellationToken);
        return ms.ToArray();
    }
}

