using LocalhostTunnel.Core.Configuration;
using LocalhostTunnel.Core.Runtime;

namespace LocalhostTunnel.Application.Services.Runtime;

public interface IRuntimeInstance
{
    string ProfileId { get; }

    TunnelProfile Profile { get; }

    ProfileRuntimeSnapshot Snapshot { get; }

    void UpdateProfile(TunnelProfile profile);

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}

