using FluentAssertions;
using LocalhostTunnel.Core.Logging;
using LocalhostTunnel.Core.Runtime;
using LocalhostTunnel.Desktop.ViewModels;

namespace LocalhostTunnel.Desktop.Tests.ViewModels;

public sealed class OverviewViewModelTests
{
    [Fact]
    public void SnapshotUpdate_Maps_Runtime_Data_To_CommandCenter_Cards()
    {
        var vm = new OverviewViewModel();
        var snapshot = new RuntimeSnapshot
        {
            Forwarder = ForwarderSnapshot.CreateRunning(DateTimeOffset.UtcNow.AddMinutes(-2), 4321),
            Tunnel = TunnelSnapshot.CreateRunning(DateTimeOffset.UtcNow.AddMinutes(-3)),
            Logs = [LogEntry.Info("tunnel", "connected")]
        };

        vm.Apply(snapshot);

        vm.ForwarderLabel.Should().Be("Running");
        vm.TunnelLabel.Should().Be("Connected");
        vm.LiveLogs.Should().ContainSingle(x => x.Message == "connected");
    }
}
