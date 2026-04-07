using CommunityToolkit.Mvvm.ComponentModel;
using LocalhostTunnel.Application.Services;
using LocalhostTunnel.Desktop.Utilities;

namespace LocalhostTunnel.Desktop.ViewModels;

public sealed partial class DiagnosticsViewModel : ObservableObject
{
    private readonly RuntimeCoordinator _runtimeCoordinator;

    [ObservableProperty]
    private string _tunnelState = "Disconnected";

    [ObservableProperty]
    private string _forwarderState = "Stopped";

    [ObservableProperty]
    private string _lastCapturedAt = "";

    public DiagnosticsViewModel(RuntimeCoordinator runtimeCoordinator)
    {
        _runtimeCoordinator = runtimeCoordinator;
        Refresh();
    }

    public void Refresh()
    {
        var snapshot = _runtimeCoordinator.Current;
        TunnelState = snapshot.Tunnel.State.ToString();
        ForwarderState = snapshot.Forwarder.State.ToString();
        LastCapturedAt = $"{AppTimeZone.Format(snapshot.CapturedAt, "yyyy-MM-dd HH:mm:ss")} ({AppTimeZone.DisplayLabel})";
    }
}
