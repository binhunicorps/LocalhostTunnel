using FluentAssertions;
using LocalhostTunnel.Core.Runtime;
using LocalhostTunnel.Infrastructure.Tunnel;

namespace LocalhostTunnel.Infrastructure.Tests.Tunnel;

public sealed class CloudflaredOutputParserTests
{
    [Theory]
    [InlineData("INF Registered tunnel connection", ServiceState.Running)]
    [InlineData("INF Connection abc123 registered connIndex=0 ip=198.41.200.23 location=sin01", ServiceState.Running)]
    [InlineData("ERR failed to serve quic connection", ServiceState.Faulted)]
    [InlineData("WARN retrying connection", ServiceState.Degraded)]
    [InlineData("INF Initial protocol quic", ServiceState.Starting)]
    public void Parse_Maps_Output_To_State(string line, ServiceState expectedState)
    {
        var parser = new CloudflaredOutputParser();

        var parsed = parser.Parse(line);

        parsed.State.Should().Be(expectedState);
    }
}
