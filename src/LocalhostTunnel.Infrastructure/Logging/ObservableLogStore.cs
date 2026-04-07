using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Core.Logging;

namespace LocalhostTunnel.Infrastructure.Logging;

public sealed class ObservableLogStore : ILogStore
{
    private readonly object _gate = new();
    private readonly List<LogEntry> _entries = new();
    private readonly int _maxEntries;

    public ObservableLogStore(int maxEntries = 500)
    {
        if (maxEntries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEntries));
        }

        _maxEntries = maxEntries;
    }

    public IReadOnlyList<LogEntry> Entries
    {
        get
        {
            lock (_gate)
            {
                return _entries.ToArray();
            }
        }
    }

    public void Append(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        lock (_gate)
        {
            _entries.Add(entry);

            var overflow = _entries.Count - _maxEntries;
            if (overflow > 0)
            {
                _entries.RemoveRange(0, overflow);
            }
        }
    }
}
