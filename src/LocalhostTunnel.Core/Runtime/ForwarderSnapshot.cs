namespace LocalhostTunnel.Core.Runtime;

public sealed record ForwarderSnapshot
{
    public ServiceState State { get; init; } = ServiceState.Stopped;

    public DateTimeOffset? StartedAt { get; init; }

    public TimeSpan Uptime { get; init; } = TimeSpan.Zero;

    public int? ProcessId { get; init; }

    public static ForwarderSnapshot CreateRunning(DateTimeOffset startedAt, int processId)
    {
        return new ForwarderSnapshot
        {
            State = ServiceState.Running,
            StartedAt = startedAt,
            ProcessId = processId,
            Uptime = DateTimeOffset.UtcNow - startedAt
        };
    }
}
