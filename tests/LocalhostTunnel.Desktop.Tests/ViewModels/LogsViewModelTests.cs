using FluentAssertions;
using LocalhostTunnel.Core.Logging;
using LocalhostTunnel.Desktop.ViewModels;
using LocalhostTunnel.Infrastructure.Logging;

namespace LocalhostTunnel.Desktop.Tests.ViewModels;

public sealed class LogsViewModelTests
{
    [Fact]
    public void ApplyFilter_Shows_Only_Selected_Log_Level()
    {
        var logStore = new ObservableLogStore();
        var vm = new LogsViewModel(logStore);
        logStore.Append(LogEntry.Info("app", "ready"));
        logStore.Append(LogEntry.Error("app", "boom"));

        vm.SelectedLevel = "error";
        vm.Refresh();

        vm.FilteredLogs.Should().ContainSingle(x => x.Level == "error");
    }
}
