using FluentAssertions;
using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Application.Services;
using LocalhostTunnel.Core.Configuration;
using LocalhostTunnel.Core.Runtime;
using LocalhostTunnel.Core.Updates;
using LocalhostTunnel.Desktop.ViewModels;
using LocalhostTunnel.Infrastructure.Logging;

namespace LocalhostTunnel.Desktop.Tests.ViewModels;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task StartCommand_DoesNotThrow_When_RuntimeStartFails()
    {
        var vm = CreateViewModel(
            config: CreateValidConfig(),
            tunnelHost: new SuccessfulTunnelHost(),
            forwarderHost: new ThrowingForwarderHost("Forwarder host is already running."));

        Func<Task> act = () => vm.StartCommand.ExecuteAsync(null);

        await act.Should().NotThrowAsync();
        vm.RuntimeStatusMessage.Should().Contain("Forwarder host is already running.");
    }

    [Fact]
    public async Task StartCommand_DoesNotThrow_When_ConfigIsInvalid()
    {
        var vm = CreateViewModel(
            config: new AppConfig(),
            tunnelHost: new SuccessfulTunnelHost(),
            forwarderHost: new SuccessfulForwarderHost());

        Func<Task> act = () => vm.StartCommand.ExecuteAsync(null);

        await act.Should().NotThrowAsync();
        vm.RuntimeStatusMessage.Should().Contain("Configuration is invalid.");
    }

    private static MainWindowViewModel CreateViewModel(
        AppConfig config,
        ITunnelHost tunnelHost,
        IForwarderHost forwarderHost)
    {
        var configStore = new InMemoryConfigStore(config);
        var logStore = new ObservableLogStore();
        var runtimeCoordinator = new RuntimeCoordinator(tunnelHost, forwarderHost, configStore, logStore);
        var navigationService = new NavigationService();
        var overview = new OverviewViewModel();
        var configuration = new ConfigurationViewModel(configStore, runtimeCoordinator);
        var logs = new LogsViewModel(logStore);
        var diagnostics = new DiagnosticsViewModel(runtimeCoordinator);
        var updates = new UpdatesViewModel(new UpdateCoordinator(new NoopUpdateService(), new NoopUpdaterLauncher()));

        return new MainWindowViewModel(
            runtimeCoordinator,
            navigationService,
            overview,
            configuration,
            logs,
            diagnostics,
            updates);
    }

    private static AppConfig CreateValidConfig()
    {
        return new AppConfig
        {
            TunnelUrl = "https://demo.trycloudflare.com/",
            TunnelToken = "token-123",
            TargetHost = "127.0.0.1",
            TargetPort = 8765,
            Host = "127.0.0.1",
            Port = 8788
        };
    }

    private sealed class InMemoryConfigStore : IConfigStore
    {
        private readonly AppConfig _config;

        public InMemoryConfigStore(AppConfig config)
        {
            _config = config;
        }

        public Task<AppConfig> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_config);
        }

        public Task SaveAsync(AppConfig config, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class SuccessfulTunnelHost : ITunnelHost
    {
        public TunnelSnapshot Current { get; private set; } = new();

        public Task<TunnelStartResult> StartAsync(string tunnelToken, CancellationToken cancellationToken)
        {
            Current = TunnelSnapshot.CreateRunning(DateTimeOffset.UtcNow);
            return Task.FromResult(new TunnelStartResult(true));
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Current = new TunnelSnapshot();
            return Task.CompletedTask;
        }
    }

    private sealed class SuccessfulForwarderHost : IForwarderHost
    {
        public ForwarderSnapshot Current { get; private set; } = new();

        public Task StartAsync(AppConfig config, CancellationToken cancellationToken)
        {
            Current = ForwarderSnapshot.CreateRunning(DateTimeOffset.UtcNow, 1234);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Current = new ForwarderSnapshot();
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingForwarderHost : IForwarderHost
    {
        private readonly string _message;

        public ThrowingForwarderHost(string message)
        {
            _message = message;
        }

        public ForwarderSnapshot Current { get; } = new();

        public Task StartAsync(AppConfig config, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(_message);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class NoopUpdateService : IUpdateService
    {
        public Task<ReleaseInfo?> CheckForUpdatesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<ReleaseInfo?>(null);
        }
    }

    private sealed class NoopUpdaterLauncher : IUpdaterLauncher
    {
        public Task LaunchAsync(ReleaseInfo release, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
