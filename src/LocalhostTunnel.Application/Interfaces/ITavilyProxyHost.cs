using LocalhostTunnel.Core.Configuration;
using LocalhostTunnel.Core.Runtime;

namespace LocalhostTunnel.Application.Interfaces;

public interface ITavilyProxyHost
{
    ServiceState CurrentState { get; }

    Task StartAsync(TavilyConfig config, CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}

