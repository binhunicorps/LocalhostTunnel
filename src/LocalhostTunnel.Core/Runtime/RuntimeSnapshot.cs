using LocalhostTunnel.Core.Logging;

namespace LocalhostTunnel.Core.Runtime;

public sealed record RuntimeSnapshot
{
    public DateTimeOffset CapturedAt { get; init; } = DateTimeOffset.UtcNow;

    public ForwarderSnapshot Forwarder { get; init; } = new();

    public TunnelSnapshot Tunnel { get; init; } = new();

    public IReadOnlyList<LogEntry> Logs { get; init; } = [];
}
