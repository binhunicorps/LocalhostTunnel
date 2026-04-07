using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Core.Updates;

namespace LocalhostTunnel.Application.Services;

public sealed class UpdateCoordinator
{
    private readonly IUpdateService _updateService;
    private readonly IUpdaterLauncher _updaterLauncher;

    public UpdateCoordinator(IUpdateService updateService, IUpdaterLauncher updaterLauncher)
    {
        _updateService = updateService;
        _updaterLauncher = updaterLauncher;
    }

    public ReleaseInfo? LatestRelease { get; private set; }

    public async Task<ReleaseInfo?> CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        LatestRelease = await _updateService.CheckForUpdatesAsync(cancellationToken);
        return LatestRelease;
    }

    public async Task<bool> LaunchUpdateAsync(CancellationToken cancellationToken)
    {
        if (LatestRelease is null)
        {
            return false;
        }

        await _updaterLauncher.LaunchAsync(LatestRelease, cancellationToken);
        return true;
    }
}
