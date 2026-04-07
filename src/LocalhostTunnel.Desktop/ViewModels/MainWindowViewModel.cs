using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalhostTunnel.Application.Services;
using LocalhostTunnel.Core.Runtime;
using LocalhostTunnel.Desktop.Utilities;
using System.Windows.Threading;

namespace LocalhostTunnel.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly RuntimeCoordinator _runtimeCoordinator;
    private readonly NavigationService _navigationService;
    private readonly ConfigurationViewModel _configurationViewModel;
    private readonly LogsViewModel _logsViewModel;
    private readonly DiagnosticsViewModel _diagnosticsViewModel;
    private readonly UpdatesViewModel _updatesViewModel;
    private readonly DispatcherTimer _uiRefreshTimer;

    [ObservableProperty]
    private string _tunnelState = ServiceState.Stopped.ToString();

    [ObservableProperty]
    private string _forwarderState = ServiceState.Stopped.ToString();

    [ObservableProperty]
    private DateTimeOffset _lastUpdatedAt = DateTimeOffset.UtcNow;

    [ObservableProperty]
    private object _currentContent = null!;

    [ObservableProperty]
    private string _currentRoute = "overview";

    [ObservableProperty]
    private string _currentSectionTitle = "Overview";

    [ObservableProperty]
    private string _currentSectionDescription = "Live runtime health and traffic activity.";

    [ObservableProperty]
    private string _tunnelStateTone = "neutral";

    [ObservableProperty]
    private string _forwarderStateTone = "neutral";

    [ObservableProperty]
    private string _runtimeStatusMessage = "Ready";

    public MainWindowViewModel(
        RuntimeCoordinator runtimeCoordinator,
        NavigationService navigationService,
        OverviewViewModel overviewViewModel,
        ConfigurationViewModel configurationViewModel,
        LogsViewModel logsViewModel,
        DiagnosticsViewModel diagnosticsViewModel,
        UpdatesViewModel updatesViewModel)
    {
        _runtimeCoordinator = runtimeCoordinator;
        _navigationService = navigationService;
        Overview = overviewViewModel;
        _configurationViewModel = configurationViewModel;
        _logsViewModel = logsViewModel;
        _diagnosticsViewModel = diagnosticsViewModel;
        _updatesViewModel = updatesViewModel;

        _runtimeCoordinator.SnapshotUpdated += OnSnapshotUpdated;
        _navigationService.RouteChanged += OnRouteChanged;

        StartCommand = new AsyncRelayCommand(StartAsync);
        StopCommand = new AsyncRelayCommand(StopAsync);
        ShowOverviewCommand = new RelayCommand(() => _navigationService.Navigate("overview"));
        ShowConfigurationCommand = new RelayCommand(() => _navigationService.Navigate("configuration"));
        ShowLogsCommand = new RelayCommand(() => _navigationService.Navigate("logs"));
        ShowDiagnosticsCommand = new RelayCommand(() => _navigationService.Navigate("diagnostics"));
        ShowUpdatesCommand = new RelayCommand(() => _navigationService.Navigate("updates"));

        CurrentContent = Overview;
        SwitchContent(_navigationService.CurrentRoute);

        _ = _configurationViewModel.LoadAsync();
        _runtimeCoordinator.PublishSnapshot();

        _uiRefreshTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _uiRefreshTimer.Tick += OnUiRefreshTick;
        _uiRefreshTimer.Start();
    }

    public OverviewViewModel Overview { get; }

    public IAsyncRelayCommand StartCommand { get; }

    public IAsyncRelayCommand StopCommand { get; }

    public IRelayCommand ShowOverviewCommand { get; }

    public IRelayCommand ShowConfigurationCommand { get; }

    public IRelayCommand ShowLogsCommand { get; }

    public IRelayCommand ShowDiagnosticsCommand { get; }

    public IRelayCommand ShowUpdatesCommand { get; }

    public string TimeZoneLabel => AppTimeZone.DisplayLabel;

    private async Task StartAsync()
    {
        RuntimeStatusMessage = "Starting runtime...";

        try
        {
            await _runtimeCoordinator.StartAsync(CancellationToken.None);
            RuntimeStatusMessage = "Runtime started.";
        }
        catch (Exception ex)
        {
            RuntimeStatusMessage = $"Start failed: {ex.Message}";
        }
        finally
        {
            Apply(_runtimeCoordinator.Current);
        }
    }

    private async Task StopAsync()
    {
        RuntimeStatusMessage = "Stopping runtime...";

        try
        {
            await _runtimeCoordinator.StopAsync(CancellationToken.None);
            RuntimeStatusMessage = "Runtime stopped.";
        }
        catch (Exception ex)
        {
            RuntimeStatusMessage = $"Stop failed: {ex.Message}";
        }
        finally
        {
            Apply(_runtimeCoordinator.Current);
        }
    }

    private void OnSnapshotUpdated(object? sender, RuntimeSnapshot snapshot)
    {
        Apply(snapshot);
    }

    private void Apply(RuntimeSnapshot snapshot)
    {
        Overview.Apply(snapshot);
        _diagnosticsViewModel.Refresh();

        TunnelState = snapshot.Tunnel.State.ToString();
        ForwarderState = snapshot.Forwarder.State.ToString();
        TunnelStateTone = ResolveTone(snapshot.Tunnel.State);
        ForwarderStateTone = ResolveTone(snapshot.Forwarder.State);
        LastUpdatedAt = snapshot.CapturedAt;
    }

    private void OnRouteChanged(object? sender, string route)
    {
        SwitchContent(route);
    }

    private void SwitchContent(string route)
    {
        CurrentRoute = route;

        CurrentContent = route switch
        {
            "configuration" => _configurationViewModel,
            "logs" => GetLogsViewModel(),
            "diagnostics" => GetDiagnosticsViewModel(),
            "updates" => _updatesViewModel,
            _ => Overview
        };

        (CurrentSectionTitle, CurrentSectionDescription) = route switch
        {
            "configuration" => ("Configuration", "Manage tunnel, forwarding, and request constraints."),
            "logs" => ("Logs", "Filter, search, and inspect runtime events."),
            "diagnostics" => ("Diagnostics", "Inspect service health and capture status snapshots."),
            "updates" => ("Updates", "Check releases and launch the updater workflow."),
            _ => ("Overview", "Live runtime health and traffic activity.")
        };
    }

    private LogsViewModel GetLogsViewModel()
    {
        _logsViewModel.Refresh();
        return _logsViewModel;
    }

    private DiagnosticsViewModel GetDiagnosticsViewModel()
    {
        _diagnosticsViewModel.Refresh();
        return _diagnosticsViewModel;
    }

    private static string ResolveTone(ServiceState state)
    {
        return state switch
        {
            ServiceState.Running => "good",
            ServiceState.Starting or ServiceState.Stopping => "warn",
            ServiceState.Degraded or ServiceState.Faulted => "bad",
            _ => "neutral"
        };
    }

    private void OnUiRefreshTick(object? sender, EventArgs e)
    {
        _runtimeCoordinator.PublishSnapshot();
        _logsViewModel.Refresh();
    }
}
