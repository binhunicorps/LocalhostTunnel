using LocalhostTunnel.Core.Updates;

namespace LocalhostTunnel.Application.Interfaces;

public interface IUpdaterLauncher
{
    Task LaunchAsync(ReleaseInfo release, CancellationToken cancellationToken);
}
