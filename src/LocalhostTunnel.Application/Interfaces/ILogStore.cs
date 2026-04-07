using LocalhostTunnel.Core.Logging;

namespace LocalhostTunnel.Application.Interfaces;

public interface ILogStore
{
    IReadOnlyList<LogEntry> Entries { get; }

    void Append(LogEntry entry);
}
