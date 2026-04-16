using FluentAssertions;
using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Application.Services;
using LocalhostTunnel.Application.Services.Runtime;
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
        var vm = await CreateViewModelAsync(
            profile: CreateValidProfile(),
            tunnelHostFactory: new SuccessfulTunnelHostFactory(),
            forwarderHostFactory: new ThrowingForwarderHostFactory("Forwarder host is already running."));

        Func<Task> act = () => vm.StartCommand.ExecuteAsync(null);

        await act.Should().NotThrowAsync();
        vm.RuntimeStatusMessage.Should().Contain("Forwarder host is already running.");
    }

    [Fact]
    public async Task StartCommand_DoesNotThrow_When_ConfigIsInvalid()
    {
        var invalidProfile = CreateValidProfile() with
        {
            TunnelToken = string.Empty
        };

        var vm = await CreateViewModelAsync(
            profile: invalidProfile,
            tunnelHostFactory: new SuccessfulTunnelHostFactory(),
            forwarderHostFactory: new SuccessfulForwarderHostFactory());

        Func<Task> act = () => vm.StartCommand.ExecuteAsync(null);

        await act.Should().NotThrowAsync();
        vm.RuntimeStatusMessage.Should().Contain("Configuration is invalid.");
    }

    private static async Task<MainWindowViewModel> CreateViewModelAsync(
        TunnelProfile profile,
        ITunnelHostFactory tunnelHostFactory,
        IForwarderHostFactory forwarderHostFactory)
    {
        var profilesStore = new InMemoryProfilesConfigStore(new ProfilesConfig
        {
            SelectedProfileId = profile.Id,
            Profiles = [profile]
        });
        var logStore = new ObservableLogStore();
        var runtimeManager = new RuntimeManager(
            profilesStore,
            tunnelHostFactory,
            forwarderHostFactory,
            new NoopTavilyProxyHostFactory(),
            logStore);
        await runtimeManager.LoadAsync(CancellationToken.None);

        var navigationService = new NavigationService();
        var overview = new OverviewViewModel();
        var configuration = new ConfigurationViewModel(runtimeManager);
        var tavily = new TavilyApiViewModel(runtimeManager);
        var logs = new LogsViewModel(logStore, runtimeManager);
        var diagnostics = new DiagnosticsViewModel(runtimeManager);
        var updates = new UpdatesViewModel(new UpdateCoordinator(new NoopUpdateService(), new NoopUpdaterLauncher()));

        return new MainWindowViewModel(
            runtimeManager,
            navigationService,
            overview,
            configuration,
            tavily,
            logs,
            diagnostics,
            updates);
    }

    private static TunnelProfile CreateValidProfile()
    {
        return new TunnelProfile
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Default",
            Type = ProfileType.Standard,
            Enabled = true,
            TunnelUrl = "https://demo.trycloudflare.com/",
            TunnelToken = "token-123",
            TargetHost = "127.0.0.1",
            TargetPort = 8765,
            Host = "127.0.0.1",
            Port = 8788
        };
    }

    private sealed class InMemoryProfilesConfigStore : IProfilesConfigStore
    {
        private readonly ProfilesConfig _config;

        public InMemoryProfilesConfigStore(ProfilesConfig config)
        {
            _config = config;
        }

        public Task<ProfilesConfig> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_config);
        }

        public Task SaveAsync(ProfilesConfig config, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class SuccessfulTunnelHostFactory : ITunnelHostFactory
    {
        public ITunnelHost Create(string profileId) => new SuccessfulTunnelHost();
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

    private sealed class SuccessfulForwarderHostFactory : IForwarderHostFactory
    {
        public IForwarderHost Create(string profileId) => new SuccessfulForwarderHost();
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

    private sealed class ThrowingForwarderHostFactory : IForwarderHostFactory
    {
        private readonly string _message;

        public ThrowingForwarderHostFactory(string message)
        {
            _message = message;
        }

        public IForwarderHost Create(string profileId) => new ThrowingForwarderHost(_message);
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

    private sealed class NoopTavilyProxyHostFactory : ITavilyProxyHostFactory
    {
        public ITavilyProxyHost Create(string profileId) => new NoopTavilyProxyHost();
    }

    private sealed class NoopTavilyProxyHost : ITavilyProxyHost
    {
        public ServiceState CurrentState => ServiceState.Stopped;

        public Task StartAsync(TavilyConfig config, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
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

