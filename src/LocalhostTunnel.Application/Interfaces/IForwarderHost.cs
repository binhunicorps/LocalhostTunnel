using LocalhostTunnel.Core.Configuration;
using LocalhostTunnel.Core.Runtime;

namespace LocalhostTunnel.Application.Interfaces;

public interface IForwarderHost
{
    ForwarderSnapshot Current { get; }

    Task StartAsync(AppConfig config, CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}
