using LocalhostTunnel.Application.Interfaces;

namespace LocalhostTunnel.Application.Services;

public sealed class StartupImportService
{
    private readonly IConfigStore? _configStore;
    private readonly IProfilesConfigStore? _profilesConfigStore;

    public StartupImportService(IConfigStore configStore)
    {
        _configStore = configStore;
    }

    public StartupImportService(IConfigStore configStore, IProfilesConfigStore profilesConfigStore)
    {
        _configStore = configStore;
        _profilesConfigStore = profilesConfigStore;
    }

    public async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_profilesConfigStore is not null)
        {
            _ = await _profilesConfigStore.LoadAsync(cancellationToken);
            return;
        }

        if (_configStore is not null)
        {
            _ = await _configStore.LoadAsync(cancellationToken);
        }
    }
}
