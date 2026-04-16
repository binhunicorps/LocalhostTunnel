namespace LocalhostTunnel.Core.Runtime;

public sealed record ProfileRuntimeSnapshot
{
    public string ProfileId { get; init; } = string.Empty;

    public string ProfileName { get; init; } = string.Empty;

    public ServiceState TunnelState { get; init; } = ServiceState.Stopped;

    public ServiceState ForwarderState { get; init; } = ServiceState.Stopped;

    public ServiceState TavilyState { get; init; } = ServiceState.Stopped;

    public DateTimeOffset CapturedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? StartedAt { get; init; }

    public TimeSpan Uptime { get; init; } = TimeSpan.Zero;

    public string LastError { get; init; } = string.Empty;

    public bool IsRunning =>
        TunnelState == ServiceState.Running ||
        ForwarderState == ServiceState.Running ||
        TavilyState == ServiceState.Running;
}

