using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalhostTunnel.Application.Services;

namespace LocalhostTunnel.Desktop.ViewModels;

public sealed partial class UpdatesViewModel : ObservableObject
{
    private readonly UpdateCoordinator _updateCoordinator;

    [ObservableProperty]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private string _latestVersion = "-";

    [ObservableProperty]
    private string _releaseNotes = "No update check yet.";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public UpdatesViewModel(UpdateCoordinator updateCoordinator)
    {
        _updateCoordinator = updateCoordinator;
        CheckForUpdatesCommand = new AsyncRelayCommand(CheckForUpdatesAsync);
        InstallUpdateCommand = new AsyncRelayCommand(InstallUpdateAsync);
    }

    public IAsyncRelayCommand CheckForUpdatesCommand { get; }

    public IAsyncRelayCommand InstallUpdateCommand { get; }

    public async Task CheckForUpdatesAsync()
    {
        StatusMessage = "Checking for updates...";
        try
        {
            var release = await _updateCoordinator.CheckForUpdatesAsync(CancellationToken.None);
            if (release is null)
            {
                IsUpdateAvailable = false;
                LatestVersion = "-";
                ReleaseNotes = "You are on the latest version.";
                StatusMessage = "No updates";
                return;
            }

            IsUpdateAvailable = true;
            LatestVersion = release.Version;
            ReleaseNotes = string.IsNullOrWhiteSpace(release.Notes) ? "Update is available." : release.Notes!;
            StatusMessage = $"Update available: {release.Version}";
        }
        catch
        {
            IsUpdateAvailable = false;
            LatestVersion = "-";
            ReleaseNotes = "Unable to check updates right now. Verify network access and release endpoint configuration.";
            StatusMessage = "Update check failed";
        }
    }

    public async Task InstallUpdateAsync()
    {
        if (!IsUpdateAvailable)
        {
            StatusMessage = "No update available.";
            return;
        }

        var launched = await _updateCoordinator.LaunchUpdateAsync(CancellationToken.None);
        StatusMessage = launched ? "Updater launched." : "Unable to launch updater.";
    }
}
