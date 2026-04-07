using LocalhostTunnel.Application.Services;
namespace LocalhostTunnel.Desktop.Services;

public sealed class WindowLifecycleService
{
    private readonly RuntimeCoordinator _runtimeCoordinator;

    public WindowLifecycleService(RuntimeCoordinator runtimeCoordinator)
    {
        _runtimeCoordinator = runtimeCoordinator;
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        await _runtimeCoordinator.StopAsync(cancellationToken);
        System.Windows.Application.Current.Shutdown();
    }
}
