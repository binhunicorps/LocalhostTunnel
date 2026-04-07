using FluentAssertions;
using LocalhostTunnel.Core.Logging;
using LocalhostTunnel.Core.Runtime;

namespace LocalhostTunnel.Core.Tests.Runtime;

public class RuntimeSnapshotTests
{
    [Fact]
    public void CreateRunning_Sets_Uptime_And_State()
    {
        var startedAt = DateTimeOffset.UtcNow.AddMinutes(-5);

        var snapshot = ForwarderSnapshot.CreateRunning(startedAt, 4321);

        snapshot.State.Should().Be(ServiceState.Running);
        snapshot.ProcessId.Should().Be(4321);
        snapshot.Uptime.Should().BeGreaterThan(TimeSpan.FromMinutes(4));
    }
}
