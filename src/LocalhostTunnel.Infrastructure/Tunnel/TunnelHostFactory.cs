using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Infrastructure.Storage;

namespace LocalhostTunnel.Infrastructure.Tunnel;

public sealed class TunnelHostFactory : ITunnelHostFactory
{
    private readonly ICloudflaredInstaller _installer;
    private readonly ILogStore _logStore;
    private readonly AppDataPaths _paths;

    public TunnelHostFactory(
        ICloudflaredInstaller installer,
        ILogStore logStore,
        AppDataPaths paths)
    {
        _installer = installer;
        _logStore = logStore;
        _paths = paths;
    }

    public ITunnelHost Create(string profileId)
    {
        return new CloudflaredProcessHost(_installer, _logStore, _paths, profileId);
    }
}

