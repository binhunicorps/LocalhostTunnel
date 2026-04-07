using LocalhostTunnel.Core.Updates;

namespace LocalhostTunnel.Application.Interfaces;

public interface IUpdateService
{
    Task<ReleaseInfo?> CheckForUpdatesAsync(CancellationToken cancellationToken);
}
