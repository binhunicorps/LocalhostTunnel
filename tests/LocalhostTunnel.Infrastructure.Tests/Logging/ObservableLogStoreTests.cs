using FluentAssertions;
using LocalhostTunnel.Core.Logging;
using LocalhostTunnel.Infrastructure.Logging;

namespace LocalhostTunnel.Infrastructure.Tests.Logging;

public class ObservableLogStoreTests
{
    [Fact]
    public void Append_Trims_Buffer_To_Maximum_Count()
    {
        var store = new ObservableLogStore(maxEntries: 3);

        store.Append(LogEntry.Info("ui", "1"));
        store.Append(LogEntry.Info("ui", "2"));
        store.Append(LogEntry.Info("ui", "3"));
        store.Append(LogEntry.Info("ui", "4"));

        store.Entries.Select(x => x.Message).Should().Equal("2", "3", "4");
    }
}
