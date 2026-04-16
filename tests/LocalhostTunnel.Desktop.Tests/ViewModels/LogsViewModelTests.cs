using FluentAssertions;
using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Application.Services.Runtime;
using LocalhostTunnel.Core.Configuration;
using LocalhostTunnel.Core.Logging;
using LocalhostTunnel.Core.Runtime;
using LocalhostTunnel.Desktop.ViewModels;
using LocalhostTunnel.Infrastructure.Logging;

namespace LocalhostTunnel.Desktop.Tests.ViewModels;

public sealed class LogsViewModelTests
{
    [Fact]
    public async Task ApplyFilter_Shows_Only_Selected_Log_Level()
    {
        var logStore = new ObservableLogStore();
        var runtimeManager = CreateRuntimeManager();
        await runtimeManager.LoadAsync(CancellationToken.None);
        var vm = new LogsViewModel(logStore, runtimeManager);

        logStore.Append(LogEntry.Info("app", "ready"));
        logStore.Append(LogEntry.Error("app", "boom"));

        vm.SelectedLevel = "error";
        vm.Refresh();

        vm.FilteredLogs.Should().ContainSingle(x => x.Level == "error");
    }

    private static RuntimeManager CreateRuntimeManager()
    {
        var profile = new TunnelProfile
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Default",
            Type = ProfileType.Standard
        };

        return new RuntimeManager(
            new InMemoryProfilesConfigStore(new ProfilesConfig
            {
                SelectedProfileId = profile.Id,
                Profiles = [profile]
            }),
            new FakeTunnelHostFactory(),
            new FakeForwarderHostFactory(),
            new FakeTavilyProxyHostFactory(),
            new ObservableLogStore());
    }

    private sealed class InMemoryProfilesConfigStore : IProfilesConfigStore
    {
        private readonly ProfilesConfig _config;

        public InMemoryProfilesConfigStore(ProfilesConfig config)
        {
            _config = config;
        }

        public Task<ProfilesConfig> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(_config);

        public Task SaveAsync(ProfilesConfig config, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeTunnelHostFactory : ITunnelHostFactory
    {
        public ITunnelHost Create(string profileId) => new FakeTunnelHost();
    }

    private sealed class FakeForwarderHostFactory : IForwarderHostFactory
    {
        public IForwarderHost Create(string profileId) => new FakeForwarderHost();
    }

    private sealed class FakeTavilyProxyHostFactory : ITavilyProxyHostFactory
    {
        public ITavilyProxyHost Create(string profileId) => new FakeTavilyHost();
    }

    private sealed class FakeTunnelHost : ITunnelHost
    {
        public TunnelSnapshot Current => new();
        public Task<TunnelStartResult> StartAsync(string tunnelToken, CancellationToken cancellationToken) => Task.FromResult(new TunnelStartResult(true));
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeForwarderHost : IForwarderHost
    {
        public ForwarderSnapshot Current => new();
        public Task StartAsync(AppConfig config, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeTavilyHost : ITavilyProxyHost
    {
        public ServiceState CurrentState => ServiceState.Stopped;
        public Task StartAsync(TavilyConfig config, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

