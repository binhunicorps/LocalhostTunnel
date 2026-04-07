using CommunityToolkit.Mvvm.ComponentModel;
using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Core.Logging;
using System.Collections.ObjectModel;

namespace LocalhostTunnel.Desktop.ViewModels;

public sealed partial class LogsViewModel : ObservableObject
{
    private readonly ILogStore _logStore;

    [ObservableProperty]
    private string _selectedLevel = "all";

    [ObservableProperty]
    private string _searchText = "";

    public LogsViewModel(ILogStore logStore)
    {
        _logStore = logStore;
        Refresh();
    }

    public IReadOnlyList<string> AvailableLevels { get; } = ["all", "info", "warn", "error"];

    public ObservableCollection<LogEntry> FilteredLogs { get; } = [];

    public void Refresh()
    {
        var query = _logStore.Entries.AsEnumerable();

        if (!string.Equals(SelectedLevel, "all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => string.Equals(x.Level, SelectedLevel, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(x => x.Message.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        FilteredLogs.Clear();
        foreach (var entry in query.TakeLast(500))
        {
            FilteredLogs.Add(entry);
        }
    }

    partial void OnSelectedLevelChanged(string value)
    {
        Refresh();
    }

    partial void OnSearchTextChanged(string value)
    {
        Refresh();
    }
}
