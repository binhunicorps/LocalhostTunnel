namespace LocalhostTunnel.Core.Logging;

public sealed record LogEntry(
    DateTimeOffset Timestamp,
    string Level,
    string Source,
    string Message)
{
    public static LogEntry Info(string source, string message) =>
        new(DateTimeOffset.UtcNow, "info", source, message);

    public static LogEntry Warn(string source, string message) =>
        new(DateTimeOffset.UtcNow, "warn", source, message);

    public static LogEntry Error(string source, string message) =>
        new(DateTimeOffset.UtcNow, "error", source, message);
}
