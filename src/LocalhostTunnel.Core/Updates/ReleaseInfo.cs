namespace LocalhostTunnel.Core.Updates;

public sealed record ReleaseInfo(
    string Version,
    string DownloadUrl,
    string? Notes = null);
