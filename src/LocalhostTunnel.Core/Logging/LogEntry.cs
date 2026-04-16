namespace LocalhostTunnel.Core.Logging;

public sealed record LogEntry(
    DateTimeOffset Timestamp,
    string Level,
    string Source,
    string Message,
    string ProfileId = "")
{
    public static LogEntry Info(string source, string message, string profileId = "") =>
        new(DateTimeOffset.UtcNow, "info", source, message, profileId);

    public static LogEntry Warn(string source, string message, string profileId = "") =>
        new(DateTimeOffset.UtcNow, "warn", source, message, profileId);

    public static LogEntry Error(string source, string message, string profileId = "") =>
        new(DateTimeOffset.UtcNow, "error", source, message, profileId);
}
