using LocalhostTunnel.Application.Interfaces;

namespace LocalhostTunnel.Application.Services;

public sealed class StartupImportService
{
    private readonly IConfigStore _configStore;

    public StartupImportService(IConfigStore configStore)
    {
        _configStore = configStore;
    }

    public async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        _ = await _configStore.LoadAsync(cancellationToken);
    }
}
