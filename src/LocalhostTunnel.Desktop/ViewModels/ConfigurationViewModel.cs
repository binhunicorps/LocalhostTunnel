using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalhostTunnel.Application.Services.Runtime;
using LocalhostTunnel.Core.Configuration;
using System.Collections.ObjectModel;

namespace LocalhostTunnel.Desktop.ViewModels;

public sealed partial class ConfigurationViewModel : ObservableObject
{
    private readonly RuntimeManager _runtimeManager;

    [ObservableProperty]
    private string _selectedProfileId = string.Empty;

    [ObservableProperty]
    private string _profileName = string.Empty;

    [ObservableProperty]
    private bool _enabled = true;

    [ObservableProperty]
    private ProfileType _profileType = ProfileType.Standard;

    [ObservableProperty]
    private string _tunnelUrl = string.Empty;

    [ObservableProperty]
    private string _tunnelToken = string.Empty;

    [ObservableProperty]
    private int _targetPort = 8765;

    [ObservableProperty]
    private int _port = 8788;

    [ObservableProperty]
    private string _host = "127.0.0.1";

    [ObservableProperty]
    private string _targetHost = "127.0.0.1";

    [ObservableProperty]
    private string _targetProtocol = "http";

    [ObservableProperty]
    private string _webhookSecret = string.Empty;

    [ObservableProperty]
    private int _maxBodySize = 10 * 1024 * 1024;

    [ObservableProperty]
    private int _upstreamTimeout = 30000;

    [ObservableProperty]
    private string _logLevel = "info";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public ConfigurationViewModel(RuntimeManager runtimeManager)
    {
        _runtimeManager = runtimeManager;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        ReloadCommand = new AsyncRelayCommand(LoadAsync);
        AddStandardProfileCommand = new AsyncRelayCommand(AddStandardProfileAsync);
        AddTavilyProfileCommand = new AsyncRelayCommand(AddTavilyProfileAsync);
        DeleteProfileCommand = new AsyncRelayCommand(DeleteProfileAsync);
        StartProfileCommand = new AsyncRelayCommand(StartProfileAsync);
        StopProfileCommand = new AsyncRelayCommand(StopProfileAsync);
    }

    public ObservableCollection<ProfileListItemViewModel> Profiles { get; } = [];

    public IReadOnlyDictionary<string, string> FieldErrors { get; private set; } = new Dictionary<string, string>();

    public IAsyncRelayCommand SaveCommand { get; }

    public IAsyncRelayCommand ReloadCommand { get; }

    public IAsyncRelayCommand AddStandardProfileCommand { get; }

    public IAsyncRelayCommand AddTavilyProfileCommand { get; }

    public IAsyncRelayCommand DeleteProfileCommand { get; }

    public IAsyncRelayCommand StartProfileCommand { get; }

    public IAsyncRelayCommand StopProfileCommand { get; }

    public async Task LoadAsync()
    {
        StatusMessage = "Loading profiles...";

        await _runtimeManager.LoadAsync(CancellationToken.None);
        BindProfiles();

        if (Profiles.Count == 0)
        {
            StatusMessage = "No profile available.";
            return;
        }

        var selectedId = _runtimeManager.SelectedProfileId;
        if (string.IsNullOrWhiteSpace(selectedId) || !Profiles.Any(x => x.Id == selectedId))
        {
            selectedId = Profiles[0].Id;
        }

        SelectedProfileId = selectedId;
        ApplyProfile(_runtimeManager.Profiles.First(x => x.Id == selectedId));
        StatusMessage = "Configuration loaded.";
    }

    public async Task SaveAsync()
    {
        var profile = BuildProfile();
        var validation = TunnelProfileValidator.Validate(profile);
        FieldErrors = new Dictionary<string, string>(validation.Errors);
        OnPropertyChanged(nameof(FieldErrors));

        if (!validation.IsValid)
        {
            StatusMessage = "Configuration is invalid. Review field errors and save again.";
            return;
        }

        var updated = _runtimeManager.Profiles
            .Select(x => string.Equals(x.Id, profile.Id, StringComparison.OrdinalIgnoreCase) ? profile : x)
            .ToArray();

        var config = new ProfilesConfig
        {
            SelectedProfileId = profile.Id,
            Profiles = updated
        };

        await _runtimeManager.SaveAsync(config, CancellationToken.None);
        await _runtimeManager.SetSelectedProfileAsync(profile.Id, CancellationToken.None);
        BindProfiles();
        StatusMessage = "Configuration saved.";
    }

    public async Task AddStandardProfileAsync()
    {
        var newProfile = new TunnelProfile
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = $"Standard {Profiles.Count(x => x.Type == ProfileType.Standard) + 1}",
            Type = ProfileType.Standard,
            Enabled = true
        };

        var next = new ProfilesConfig
        {
            SelectedProfileId = newProfile.Id,
            Profiles = _runtimeManager.Profiles.Concat([newProfile]).ToArray()
        };

        await _runtimeManager.SaveAsync(next, CancellationToken.None);
        BindProfiles();
        SelectedProfileId = newProfile.Id;
        ApplyProfile(newProfile);
        StatusMessage = "Standard profile added.";
    }

    public async Task AddTavilyProfileAsync()
    {
        var newProfile = new TunnelProfile
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = $"Tavily {Profiles.Count(x => x.Type == ProfileType.Tavily) + 1}",
            Type = ProfileType.Tavily,
            Enabled = true,
            TargetHost = "127.0.0.1",
            TargetPort = 8766,
            Port = 8789,
            Tavily = new TavilyConfig()
        };

        var next = new ProfilesConfig
        {
            SelectedProfileId = newProfile.Id,
            Profiles = _runtimeManager.Profiles.Concat([newProfile]).ToArray()
        };

        await _runtimeManager.SaveAsync(next, CancellationToken.None);
        BindProfiles();
        SelectedProfileId = newProfile.Id;
        ApplyProfile(newProfile);
        StatusMessage = "Tavily profile added.";
    }

    public async Task DeleteProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedProfileId))
        {
            return;
        }

        await _runtimeManager.RemoveProfileAsync(SelectedProfileId, CancellationToken.None);
        BindProfiles();

        if (Profiles.Count > 0)
        {
            SelectedProfileId = Profiles[0].Id;
            ApplyProfile(_runtimeManager.Profiles.First(x => x.Id == SelectedProfileId));
        }

        StatusMessage = "Profile removed.";
    }

    public async Task StartProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedProfileId))
        {
            return;
        }

        await SaveAsync();
        try
        {
            await _runtimeManager.StartProfileAsync(SelectedProfileId, CancellationToken.None);
            StatusMessage = "Profile started.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Start failed: {ex.Message}";
        }
    }

    public async Task StopProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedProfileId))
        {
            return;
        }

        await _runtimeManager.StopProfileAsync(SelectedProfileId, CancellationToken.None);
        StatusMessage = "Profile stopped.";
    }

    partial void OnSelectedProfileIdChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var profile = _runtimeManager.Profiles.FirstOrDefault(x => string.Equals(x.Id, value, StringComparison.OrdinalIgnoreCase));
        if (profile is null)
        {
            return;
        }

        _ = _runtimeManager.SetSelectedProfileAsync(profile.Id, CancellationToken.None);
        ApplyProfile(profile);
    }

    private void ApplyProfile(TunnelProfile profile)
    {
        ProfileName = profile.Name;
        Enabled = profile.Enabled;
        ProfileType = profile.Type;
        TunnelUrl = profile.TunnelUrl;
        TunnelToken = profile.TunnelToken;
        TargetPort = profile.TargetPort;
        Port = profile.Port;
        Host = profile.Host;
        TargetHost = profile.TargetHost;
        TargetProtocol = profile.TargetProtocol;
        WebhookSecret = profile.WebhookSecret;
        MaxBodySize = profile.MaxBodySize;
        UpstreamTimeout = profile.UpstreamTimeout;
        LogLevel = profile.LogLevel;
    }

    private void BindProfiles()
    {
        Profiles.Clear();
        foreach (var profile in _runtimeManager.Profiles)
        {
            Profiles.Add(new ProfileListItemViewModel
            {
                Id = profile.Id,
                Name = profile.Name,
                Type = profile.Type,
                Enabled = profile.Enabled
            });
        }
    }

    private TunnelProfile BuildProfile()
    {
        var existing = _runtimeManager.Profiles.First(x => string.Equals(x.Id, SelectedProfileId, StringComparison.OrdinalIgnoreCase));
        return existing with
        {
            Name = string.IsNullOrWhiteSpace(ProfileName) ? existing.Name : ProfileName.Trim(),
            Enabled = Enabled,
            Type = ProfileType,
            TunnelUrl = TunnelUrl.Trim(),
            TunnelToken = TunnelToken.Trim(),
            TargetPort = TargetPort,
            Port = Port,
            Host = Host.Trim(),
            TargetHost = TargetHost.Trim(),
            TargetProtocol = TargetProtocol.Trim(),
            WebhookSecret = WebhookSecret,
            MaxBodySize = MaxBodySize,
            UpstreamTimeout = UpstreamTimeout,
            LogLevel = LogLevel.Trim()
        };
    }
}

