using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalhostTunnel.Application.Services.Runtime;
using LocalhostTunnel.Core.Configuration;
using System.Collections.ObjectModel;

namespace LocalhostTunnel.Desktop.ViewModels;

public sealed partial class TavilyApiViewModel : ObservableObject
{
    private readonly RuntimeManager _runtimeManager;

    [ObservableProperty]
    private string _selectedProfileId = string.Empty;

    [ObservableProperty]
    private string _profileName = string.Empty;

    [ObservableProperty]
    private bool _enabled = true;

    [ObservableProperty]
    private string _host = "127.0.0.1";

    [ObservableProperty]
    private int _port = 8766;

    [ObservableProperty]
    private int _forwarderPort = 8789;

    [ObservableProperty]
    private string _baseUrl = "https://api.tavily.com";

    [ObservableProperty]
    private double _requestTimeoutSeconds = 60;

    [ObservableProperty]
    private string _apiKey1 = string.Empty;

    [ObservableProperty]
    private string _apiKey2 = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _runtimeState = "Stopped";

    public TavilyApiViewModel(RuntimeManager runtimeManager)
    {
        _runtimeManager = runtimeManager;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        ReloadCommand = new AsyncRelayCommand(LoadAsync);
        StartCommand = new AsyncRelayCommand(StartAsync);
        StopCommand = new AsyncRelayCommand(StopAsync);
    }

    public ObservableCollection<ProfileListItemViewModel> TavilyProfiles { get; } = [];

    public IAsyncRelayCommand SaveCommand { get; }

    public IAsyncRelayCommand ReloadCommand { get; }

    public IAsyncRelayCommand StartCommand { get; }

    public IAsyncRelayCommand StopCommand { get; }

    public Task LoadAsync()
    {
        StatusMessage = "Loading Tavily profiles...";

        var profiles = _runtimeManager.Profiles
            .Where(x => x.Type == ProfileType.Tavily)
            .ToArray();

        TavilyProfiles.Clear();
        foreach (var profile in profiles)
        {
            TavilyProfiles.Add(ToListItem(profile));
        }

        if (profiles.Length == 0)
        {
            StatusMessage = "No Tavily profile found. Add one in Configuration tab.";
            SelectedProfileId = string.Empty;
            RuntimeState = "Stopped";
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(SelectedProfileId) ||
            !profiles.Any(x => string.Equals(x.Id, SelectedProfileId, StringComparison.OrdinalIgnoreCase)))
        {
            SelectedProfileId = profiles[0].Id;
        }

        ApplyFromProfile(profiles.First(x => string.Equals(x.Id, SelectedProfileId, StringComparison.OrdinalIgnoreCase)));
        StatusMessage = "Tavily profile loaded.";
        return Task.CompletedTask;
    }

    public async Task SaveAsync()
    {
        var profile = BuildProfile();
        var profiles = _runtimeManager.Profiles.ToArray();

        var updated = profiles
            .Select(x => string.Equals(x.Id, profile.Id, StringComparison.OrdinalIgnoreCase) ? profile : x)
            .ToArray();

        var nextConfig = new ProfilesConfig
        {
            SelectedProfileId = _runtimeManager.SelectedProfileId,
            Profiles = updated
        };

        await _runtimeManager.SaveAsync(nextConfig, CancellationToken.None);
        StatusMessage = "Tavily profile saved.";
        await LoadAsync();
    }

    public async Task StartAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedProfileId))
        {
            StatusMessage = "No Tavily profile is selected.";
            return;
        }

        await SaveAsync();

        try
        {
            await _runtimeManager.StartProfileAsync(SelectedProfileId, CancellationToken.None);
            RuntimeState = _runtimeManager.GetProfileSnapshot(SelectedProfileId).TavilyState.ToString();
            StatusMessage = "Tavily runtime started.";
        }
        catch (Exception ex)
        {
            RuntimeState = _runtimeManager.GetProfileSnapshot(SelectedProfileId).TavilyState.ToString();
            StatusMessage = $"Start failed: {ex.Message}";
        }
    }

    public async Task StopAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedProfileId))
        {
            StatusMessage = "No Tavily profile is selected.";
            return;
        }

        await _runtimeManager.StopProfileAsync(SelectedProfileId, CancellationToken.None);
        RuntimeState = _runtimeManager.GetProfileSnapshot(SelectedProfileId).TavilyState.ToString();
        StatusMessage = "Tavily runtime stopped.";
    }

    partial void OnSelectedProfileIdChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var profile = _runtimeManager.Profiles
            .FirstOrDefault(x => string.Equals(x.Id, value, StringComparison.OrdinalIgnoreCase) && x.Type == ProfileType.Tavily);
        if (profile is null)
        {
            return;
        }

        ApplyFromProfile(profile);
    }

    private void ApplyFromProfile(TunnelProfile profile)
    {
        var tavily = profile.Tavily ?? new TavilyConfig();

        ProfileName = profile.Name;
        Enabled = profile.Enabled;

        Host = tavily.Host;
        Port = tavily.Port;
        ForwarderPort = profile.Port;
        BaseUrl = tavily.BaseUrl;
        RequestTimeoutSeconds = tavily.RequestTimeoutSeconds;
        ApiKey1 = tavily.ApiKey1;
        ApiKey2 = tavily.ApiKey2;

        RuntimeState = _runtimeManager.GetProfileSnapshot(profile.Id).TavilyState.ToString();
    }

    private TunnelProfile BuildProfile()
    {
        var existing = _runtimeManager.Profiles
            .FirstOrDefault(x => string.Equals(x.Id, SelectedProfileId, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            throw new InvalidOperationException("Selected Tavily profile was not found.");
        }

        return existing with
        {
            Name = string.IsNullOrWhiteSpace(ProfileName) ? "Tavily API" : ProfileName.Trim(),
            Type = ProfileType.Tavily,
            Enabled = Enabled,
            TargetPort = Port,
            Port = ForwarderPort,
            Tavily = new TavilyConfig
            {
                ApiKey1 = ApiKey1.Trim(),
                ApiKey2 = ApiKey2.Trim(),
                Host = Host.Trim(),
                Port = Port,
                BaseUrl = BaseUrl.Trim(),
                RequestTimeoutSeconds = RequestTimeoutSeconds
            }
        };
    }

    private static ProfileListItemViewModel ToListItem(TunnelProfile profile)
    {
        return new ProfileListItemViewModel
        {
            Id = profile.Id,
            Name = profile.Name,
            Type = profile.Type,
            Enabled = profile.Enabled
        };
    }
}
