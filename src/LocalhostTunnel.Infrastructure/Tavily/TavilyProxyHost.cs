using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Core.Configuration;
using LocalhostTunnel.Core.Logging;
using LocalhostTunnel.Core.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace LocalhostTunnel.Infrastructure.Tavily;

public sealed class TavilyProxyHost : ITavilyProxyHost
{
    private readonly object _sync = new();

    private readonly ILogStore _logStore;
    private readonly string _profileId;

    private WebApplication? _webApplication;
    private ServiceState _currentState = ServiceState.Stopped;
    private DateTimeOffset? _startedAt;

    public TavilyProxyHost(ILogStore logStore)
        : this(logStore, string.Empty)
    {
    }

    public TavilyProxyHost(ILogStore logStore, string profileId)
    {
        _logStore = logStore;
        _profileId = profileId;
    }

    public ServiceState CurrentState
    {
        get
        {
            lock (_sync)
            {
                return _currentState;
            }
        }
    }

    public async Task StartAsync(TavilyConfig config, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);

        lock (_sync)
        {
            if (_webApplication is not null)
            {
                return;
            }

            _currentState = ServiceState.Starting;
        }

        WebApplication app;

        try
        {
            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(config.RequestTimeoutSeconds)
            };

            var keyManager = new TavilyKeyManager(config.ApiKey1, config.ApiKey2);
            var proxyService = new TavilyProxyService(config.BaseUrl, keyManager, httpClient, _logStore, _profileId);

            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseUrls($"http://{config.Host}:{config.Port}");

            app = builder.Build();
            app.MapGet("/health", () => Results.Json(new
            {
                status = "ok",
                uptime = _startedAt.HasValue ? (DateTimeOffset.UtcNow - _startedAt.Value).TotalSeconds : 0
            }));
            app.Map("/{**path}", context => proxyService.HandleAsync(context, context.RequestAborted));

            await app.StartAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            lock (_sync)
            {
                _currentState = ServiceState.Faulted;
                _startedAt = null;
            }

            _logStore.Append(LogEntry.Error("tavily", $"failed to start tavily proxy: {ex.Message}", _profileId));
            throw;
        }

        lock (_sync)
        {
            _webApplication = app;
            _startedAt = DateTimeOffset.UtcNow;
            _currentState = ServiceState.Running;
        }

        _logStore.Append(LogEntry.Info("tavily", $"tavily proxy is running at http://{config.Host}:{config.Port}", _profileId));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        WebApplication? appToStop;
        lock (_sync)
        {
            appToStop = _webApplication;
            _currentState = ServiceState.Stopping;
            _webApplication = null;
        }

        if (appToStop is not null)
        {
            await appToStop.StopAsync(cancellationToken);
            await appToStop.DisposeAsync();
        }

        lock (_sync)
        {
            _startedAt = null;
            _currentState = ServiceState.Stopped;
        }
    }
}
