using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Core.Configuration;
using LocalhostTunnel.Core.Logging;
using LocalhostTunnel.Core.Runtime;

namespace LocalhostTunnel.Application.Services.Runtime;

public sealed class StandardRuntimeInstance : IRuntimeInstance
{
    private readonly ITunnelHost _tunnelHost;
    private readonly IForwarderHost _forwarderHost;
    private readonly ILogStore _logStore;

    private readonly object _sync = new();
    private TunnelProfile _profile;
    private DateTimeOffset? _startedAt;
    private string _lastError = string.Empty;

    public StandardRuntimeInstance(
        TunnelProfile profile,
        ITunnelHost tunnelHost,
        IForwarderHost forwarderHost,
        ILogStore logStore)
    {
        _profile = profile;
        _tunnelHost = tunnelHost;
        _forwarderHost = forwarderHost;
        _logStore = logStore;
    }

    public string ProfileId => _profile.Id;

    public TunnelProfile Profile
    {
        get
        {
            lock (_sync)
            {
                return _profile;
            }
        }
    }

    public ProfileRuntimeSnapshot Snapshot
    {
        get
        {
            var tunnel = _tunnelHost.Current;
            var forwarder = _forwarderHost.Current;

            var startedAt = ResolveStartedAt(tunnel.StartedAt, forwarder.StartedAt);
            var isActive = tunnel.State != ServiceState.Stopped || forwarder.State != ServiceState.Stopped;
            var uptime = startedAt.HasValue && isActive
                ? DateTimeOffset.UtcNow - startedAt.Value
                : TimeSpan.Zero;

            lock (_sync)
            {
                return new ProfileRuntimeSnapshot
                {
                    ProfileId = _profile.Id,
                    ProfileName = _profile.Name,
                    TunnelState = tunnel.State,
                    ForwarderState = forwarder.State,
                    TavilyState = ServiceState.Stopped,
                    CapturedAt = DateTimeOffset.UtcNow,
                    StartedAt = startedAt,
                    Uptime = uptime,
                    LastError = _lastError
                };
            }
        }
    }

    public void UpdateProfile(TunnelProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        lock (_sync)
        {
            _profile = profile;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        TunnelProfile profile;
        lock (_sync)
        {
            profile = _profile;
            _lastError = string.Empty;
        }

        var validation = TunnelProfileValidator.Validate(profile);
        if (!validation.IsValid)
        {
            var details = string.Join("; ", validation.Errors.Select(x => $"{x.Key}: {x.Value}"));
            throw new InvalidOperationException($"Configuration is invalid. {details}");
        }

        var tunnelResult = await _tunnelHost.StartAsync(profile.TunnelToken, cancellationToken);
        if (!tunnelResult.IsSuccess)
        {
            SetLastError(tunnelResult.ErrorMessage ?? "Failed to start tunnel host.");
            throw new InvalidOperationException(tunnelResult.ErrorMessage ?? "Failed to start tunnel host.");
        }

        try
        {
            await _forwarderHost.StartAsync(profile.ToAppConfig(), cancellationToken);
            lock (_sync)
            {
                _startedAt = DateTimeOffset.UtcNow;
                _lastError = string.Empty;
            }
        }
        catch (Exception ex)
        {
            SetLastError(ex.Message);
            _logStore.Append(LogEntry.Error("runtime", $"profile '{profile.Name}' failed to start forwarder: {ex.Message}", profile.Id));
            await _tunnelHost.StopAsync(cancellationToken);
            throw;
        }
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
            lock (_sync)
            {
                _startedAt = null;
            }
        }
    }

    private void SetLastError(string message)
    {
        lock (_sync)
        {
            _lastError = message;
        }
    }

    private DateTimeOffset? ResolveStartedAt(DateTimeOffset? tunnelStartedAt, DateTimeOffset? forwarderStartedAt)
    {
        DateTimeOffset? localStartedAt;
        lock (_sync)
        {
            localStartedAt = _startedAt;
        }

        var startedCandidates = new[] { localStartedAt, tunnelStartedAt, forwarderStartedAt }
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .OrderBy(x => x)
            .ToArray();

        return startedCandidates.Length == 0 ? null : startedCandidates[0];
    }
}

