using LocalhostTunnel.Core.Runtime;

namespace LocalhostTunnel.Infrastructure.Tunnel;

public sealed class CloudflaredOutputParser
{
    public CloudflaredOutput Parse(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return new CloudflaredOutput(ServiceState.Starting, string.Empty);
        }

        if (line.Contains("Registered tunnel connection", StringComparison.OrdinalIgnoreCase))
        {
            return new CloudflaredOutput(ServiceState.Running, line);
        }

        if (line.Contains("registered", StringComparison.OrdinalIgnoreCase) &&
            line.Contains("connindex", StringComparison.OrdinalIgnoreCase))
        {
            return new CloudflaredOutput(ServiceState.Running, line);
        }

        if (line.StartsWith("ERR", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("error", StringComparison.OrdinalIgnoreCase))
        {
            return new CloudflaredOutput(ServiceState.Faulted, line);
        }

        if (line.StartsWith("WARN", StringComparison.OrdinalIgnoreCase))
        {
            return new CloudflaredOutput(ServiceState.Degraded, line);
        }

        return new CloudflaredOutput(ServiceState.Starting, line);
    }
}

public sealed record CloudflaredOutput(ServiceState State, string Line);
