using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Core.Configuration;
using LocalhostTunnel.Core.Runtime;

namespace LocalhostTunnel.Application.Services;

public sealed class RuntimeCoordinator
{
    private readonly ITunnelHost _tunnelHost;
    private readonly IForwarderHost _forwarderHost;
    private readonly IConfigStore _configStore;
    private readonly ILogStore _logStore;
    private readonly ICloudflaredInstaller _cloudflaredInstaller;

    public RuntimeCoordinator(
        ITunnelHost tunnelHost,
        IForwarderHost forwarderHost,
        IConfigStore configStore,
        ILogStore logStore)
        : this(tunnelHost, forwarderHost, configStore, logStore, NoopCloudflaredInstaller.Instance)
    {
    }

    public RuntimeCoordinator(
        ITunnelHost tunnelHost,
        IForwarderHost forwarderHost,
        IConfigStore configStore,
        ILogStore logStore,
        ICloudflaredInstaller cloudflaredInstaller)
    {
        _tunnelHost = tunnelHost;
        _forwarderHost = forwarderHost;
        _configStore = configStore;
        _logStore = logStore;
        _cloudflaredInstaller = cloudflaredInstaller;
    }

    public RuntimeSnapshot Current { get; private set; } = new();

    public event EventHandler<RuntimeSnapshot>? SnapshotUpdated;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var config = await _configStore.LoadAsync(cancellationToken);
        var validation = AppConfigValidator.Validate(config);
        if (!validation.IsValid)
        {
            var details = string.Join("; ", validation.Errors.Select(x => $"{x.Key}: {x.Value}"));
            throw new InvalidOperationException($"Configuration is invalid. {details}");
        }

        await _cloudflaredInstaller.EnsureInstalledAsync(cancellationToken);

        var tunnelResult = await _tunnelHost.StartAsync(config.TunnelToken, cancellationToken);
        if (!tunnelResult.IsSuccess)
        {
            throw new InvalidOperationException(tunnelResult.ErrorMessage ?? "Failed to start tunnel host.");
        }

        await _forwarderHost.StartAsync(config, cancellationToken);
        PublishSnapshot();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _forwarderHost.StopAsync(cancellationToken);
        }
        finally
        {
            await _tunnelHost.StopAsync(cancellationToken);
            PublishSnapshot();
        }
    }

    public async Task ReloadConfigAsync(CancellationToken cancellationToken)
    {
        await StopAsync(cancellationToken);
        await StartAsync(cancellationToken);
    }

    public void PublishSnapshot()
    {
        Current = new RuntimeSnapshot
        {
            CapturedAt = DateTimeOffset.UtcNow,
            Forwarder = _forwarderHost.Current,
            Tunnel = _tunnelHost.Current,
            Logs = _logStore.Entries.ToArray()
        };

        SnapshotUpdated?.Invoke(this, Current);
    }

    private sealed class NoopCloudflaredInstaller : ICloudflaredInstaller
    {
        public static readonly NoopCloudflaredInstaller Instance = new();

        public Task EnsureInstalledAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
