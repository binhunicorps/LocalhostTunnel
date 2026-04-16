using CommunityToolkit.Mvvm.ComponentModel;
using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Application.Services.Runtime;
using LocalhostTunnel.Core.Logging;
using System.Collections.ObjectModel;

namespace LocalhostTunnel.Desktop.ViewModels;

public sealed partial class LogsViewModel : ObservableObject
{
    private readonly ILogStore _logStore;
    private readonly RuntimeManager _runtimeManager;

    [ObservableProperty]
    private string _selectedLevel = "all";

    [ObservableProperty]
    private string _selectedProfileId = "all";

    [ObservableProperty]
    private string _searchText = string.Empty;

    public LogsViewModel(ILogStore logStore, RuntimeManager runtimeManager)
    {
        _logStore = logStore;
        _runtimeManager = runtimeManager;
        Refresh();
    }

    public IReadOnlyList<string> AvailableLevels { get; } = ["all", "info", "warn", "error"];

    public ObservableCollection<ProfileListItemViewModel> AvailableProfiles { get; } = [];

    public ObservableCollection<LogEntry> FilteredLogs { get; } = [];

    public void Refresh()
    {
        BindProfiles();

        var query = _logStore.Entries.AsEnumerable();

        if (!string.Equals(SelectedProfileId, "all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x =>
                string.IsNullOrWhiteSpace(x.ProfileId) ||
                string.Equals(x.ProfileId, SelectedProfileId, StringComparison.OrdinalIgnoreCase));
        }

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

    partial void OnSelectedProfileIdChanged(string value)
    {
        Refresh();
    }

    partial void OnSearchTextChanged(string value)
    {
        Refresh();
    }

    private void BindProfiles()
    {
        var items = _runtimeManager.Profiles
            .Select(x => new ProfileListItemViewModel
            {
                Id = x.Id,
                Name = x.Name,
                Type = x.Type,
                Enabled = x.Enabled
            })
            .ToArray();

        AvailableProfiles.Clear();
        AvailableProfiles.Add(new ProfileListItemViewModel
        {
            Id = "all",
            Name = "All Profiles",
            Type = Core.Configuration.ProfileType.Standard,
            Enabled = true
        });

        foreach (var item in items)
        {
            AvailableProfiles.Add(item);
        }

        if (string.IsNullOrWhiteSpace(SelectedProfileId) || !AvailableProfiles.Any(x => x.Id == SelectedProfileId))
        {
            SelectedProfileId = "all";
        }
    }
}

