using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Application.Services;
using LocalhostTunnel.Core.Configuration;
using LocalhostTunnel.Core.Runtime;

namespace LocalhostTunnel.Desktop.ViewModels;

public sealed partial class ConfigurationViewModel : ObservableObject
{
    private readonly IConfigStore _configStore;
    private readonly RuntimeCoordinator _runtimeCoordinator;

    [ObservableProperty]
    private string _tunnelUrl = "";

    [ObservableProperty]
    private string _tunnelToken = "";

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
    private string _webhookSecret = "";

    [ObservableProperty]
    private int _maxBodySize = 10 * 1024 * 1024;

    [ObservableProperty]
    private int _upstreamTimeout = 30000;

    [ObservableProperty]
    private string _logLevel = "info";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public ConfigurationViewModel(IConfigStore configStore, RuntimeCoordinator runtimeCoordinator)
    {
        _configStore = configStore;
        _runtimeCoordinator = runtimeCoordinator;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        ReloadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public IReadOnlyDictionary<string, string> FieldErrors { get; private set; } = new Dictionary<string, string>();

    public IAsyncRelayCommand SaveCommand { get; }

    public IAsyncRelayCommand ReloadCommand { get; }

    public async Task LoadAsync()
    {
        StatusMessage = "Loading configuration...";

        try
        {
            var config = await _configStore.LoadAsync(CancellationToken.None);

            TunnelUrl = config.TunnelUrl;
            TunnelToken = config.TunnelToken;
            TargetPort = config.TargetPort;
            Port = config.Port;
            Host = config.Host;
            TargetHost = config.TargetHost;
            TargetProtocol = config.TargetProtocol;
            WebhookSecret = config.WebhookSecret;
            MaxBodySize = config.MaxBodySize;
            UpstreamTimeout = config.UpstreamTimeout;
            LogLevel = config.LogLevel;
            StatusMessage = "Configuration loaded.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unable to load configuration: {ex.Message}";
        }
    }

    public async Task SaveAsync()
    {
        StatusMessage = "Validating configuration...";

        var config = BuildConfig();
        var validation = AppConfigValidator.Validate(config);
        FieldErrors = new Dictionary<string, string>(validation.Errors);
        OnPropertyChanged(nameof(FieldErrors));

        if (!validation.IsValid)
        {
            StatusMessage = "Configuration is invalid. Review field errors and save again.";
            return;
        }

        await _configStore.SaveAsync(config, CancellationToken.None);
        StatusMessage = "Configuration saved.";

        if (!ShouldReloadRuntime(_runtimeCoordinator.Current))
        {
            return;
        }

        try
        {
            await _runtimeCoordinator.ReloadConfigAsync(CancellationToken.None);
            StatusMessage = "Configuration saved and runtime reloaded.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Configuration saved. Runtime reload failed: {ex.Message}";
        }
    }

    private static bool ShouldReloadRuntime(RuntimeSnapshot snapshot)
    {
        return snapshot.Tunnel.State != ServiceState.Stopped ||
               snapshot.Forwarder.State != ServiceState.Stopped;
    }

    private AppConfig BuildConfig()
    {
        return new AppConfig
        {
            TunnelUrl = TunnelUrl,
            TunnelToken = TunnelToken,
            TargetPort = TargetPort,
            Port = Port,
            Host = Host,
            TargetHost = TargetHost,
            TargetProtocol = TargetProtocol,
            WebhookSecret = WebhookSecret,
            MaxBodySize = MaxBodySize,
            UpstreamTimeout = UpstreamTimeout,
            LogLevel = LogLevel
        };
    }
}
