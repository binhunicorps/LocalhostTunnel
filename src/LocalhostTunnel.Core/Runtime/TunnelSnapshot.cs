namespace LocalhostTunnel.Core.Runtime;

public sealed record TunnelSnapshot
{
    public ServiceState State { get; init; } = ServiceState.Stopped;

    public DateTimeOffset? StartedAt { get; init; }

    public TimeSpan Uptime { get; init; } = TimeSpan.Zero;

    public static TunnelSnapshot CreateRunning(DateTimeOffset startedAt)
    {
        return new TunnelSnapshot
        {
            State = ServiceState.Running,
            StartedAt = startedAt,
            Uptime = DateTimeOffset.UtcNow - startedAt
        };
    }
}
