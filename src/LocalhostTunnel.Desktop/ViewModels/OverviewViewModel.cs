using CommunityToolkit.Mvvm.ComponentModel;
using LocalhostTunnel.Core.Logging;
using LocalhostTunnel.Core.Runtime;
using System.Collections.ObjectModel;

namespace LocalhostTunnel.Desktop.ViewModels;

public sealed partial class OverviewViewModel : ObservableObject
{
    [ObservableProperty]
    private string _forwarderLabel = "Stopped";

    [ObservableProperty]
    private string _tunnelLabel = "Disconnected";

    [ObservableProperty]
    private string _uptimeLabel = "00:00:00";

    public ObservableCollection<LogEntry> LiveLogs { get; } = [];

    public void Apply(RuntimeSnapshot snapshot)
    {
        ForwarderLabel = snapshot.Forwarder.State switch
        {
            ServiceState.Running => "Running",
            ServiceState.Starting => "Starting",
            ServiceState.Degraded => "Degraded",
            ServiceState.Faulted => "Faulted",
            ServiceState.Stopping => "Stopping",
            _ => "Stopped"
        };

        TunnelLabel = snapshot.Tunnel.State switch
        {
            ServiceState.Running => "Connected",
            ServiceState.Starting => "Connecting",
            ServiceState.Degraded => "Degraded",
            ServiceState.Faulted => "Faulted",
            ServiceState.Stopping => "Stopping",
            _ => "Disconnected"
        };

        var uptime = snapshot.Forwarder.Uptime > TimeSpan.Zero
            ? snapshot.Forwarder.Uptime
            : snapshot.Tunnel.Uptime;
        UptimeLabel = uptime.ToString(@"hh\:mm\:ss");

        LiveLogs.Clear();
        foreach (var entry in snapshot.Logs.TakeLast(40))
        {
            LiveLogs.Add(entry);
        }
    }
}
