using CommunityToolkit.Mvvm.ComponentModel;
using LocalhostTunnel.Application.Services.Runtime;
using LocalhostTunnel.Desktop.Utilities;

namespace LocalhostTunnel.Desktop.ViewModels;

public sealed partial class DiagnosticsViewModel : ObservableObject
{
    private readonly RuntimeManager _runtimeManager;

    [ObservableProperty]
    private string _profileName = "N/A";

    [ObservableProperty]
    private string _tunnelState = "Disconnected";

    [ObservableProperty]
    private string _forwarderState = "Stopped";

    [ObservableProperty]
    private string _tavilyState = "Stopped";

    [ObservableProperty]
    private string _lastCapturedAt = string.Empty;

    public DiagnosticsViewModel(RuntimeManager runtimeManager)
    {
        _runtimeManager = runtimeManager;
        Refresh();
    }

    public void Refresh()
    {
        var snapshot = _runtimeManager.Current;
        var profile = snapshot.Profiles.FirstOrDefault(x => x.ProfileId == snapshot.SelectedProfileId);

        ProfileName = profile?.ProfileName ?? "N/A";
        TunnelState = profile?.TunnelState.ToString() ?? "Disconnected";
        ForwarderState = profile?.ForwarderState.ToString() ?? "Stopped";
        TavilyState = profile?.TavilyState.ToString() ?? "Stopped";
        LastCapturedAt = $"{AppTimeZone.Format(snapshot.CapturedAt, "yyyy-MM-dd HH:mm:ss")} ({AppTimeZone.DisplayLabel})";
    }
}

