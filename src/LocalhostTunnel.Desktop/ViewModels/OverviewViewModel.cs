using CommunityToolkit.Mvvm.ComponentModel;
using LocalhostTunnel.Core.Logging;
using LocalhostTunnel.Core.Runtime;
using System.Collections.ObjectModel;

namespace LocalhostTunnel.Desktop.ViewModels;

public sealed partial class OverviewViewModel : ObservableObject
{
    [ObservableProperty]
    private string _activeProfileName = "Default";

    [ObservableProperty]
    private string _forwarderLabel = "Stopped";

    [ObservableProperty]
    private string _tunnelLabel = "Disconnected";

    [ObservableProperty]
    private string _uptimeLabel = "00:00:00";

    public ObservableCollection<LogEntry> LiveLogs { get; } = [];

    public void Apply(RuntimeSnapshot snapshot)
    {
        var selected = snapshot.Profiles
            .FirstOrDefault(x => string.Equals(x.ProfileId, snapshot.SelectedProfileId, StringComparison.OrdinalIgnoreCase));

        var forwarderState = selected?.ForwarderState ?? snapshot.Forwarder.State;
        var tunnelState = selected?.TunnelState ?? snapshot.Tunnel.State;
        var uptime = selected?.Uptime ??
                     (snapshot.Forwarder.Uptime > TimeSpan.Zero ? snapshot.Forwarder.Uptime : snapshot.Tunnel.Uptime);

        ActiveProfileName = selected?.ProfileName ?? "Default";

        ForwarderLabel = forwarderState switch
        {
            ServiceState.Running => "Running",
            ServiceState.Starting => "Starting",
            ServiceState.Degraded => "Degraded",
            ServiceState.Faulted => "Faulted",
            ServiceState.Stopping => "Stopping",
            _ => "Stopped"
        };

        TunnelLabel = tunnelState switch
        {
            ServiceState.Running => "Connected",
            ServiceState.Starting => "Connecting",
            ServiceState.Degraded => "Degraded",
            ServiceState.Faulted => "Faulted",
            ServiceState.Stopping => "Stopping",
            _ => "Disconnected"
        };

        UptimeLabel = uptime.ToString(@"hh\:mm\:ss");

        var filteredLogs = snapshot.Logs.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(snapshot.SelectedProfileId))
        {
            filteredLogs = filteredLogs.Where(x =>
                string.IsNullOrWhiteSpace(x.ProfileId) ||
                string.Equals(x.ProfileId, snapshot.SelectedProfileId, StringComparison.OrdinalIgnoreCase));
        }

        LiveLogs.Clear();
        foreach (var entry in filteredLogs.TakeLast(40))
        {
            LiveLogs.Add(entry);
        }
    }
}
