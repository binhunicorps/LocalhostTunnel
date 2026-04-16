using FluentAssertions;
using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Application.Services.Runtime;
using LocalhostTunnel.Core.Configuration;
using LocalhostTunnel.Core.Runtime;
using LocalhostTunnel.Desktop.ViewModels;
using LocalhostTunnel.Infrastructure.Logging;

namespace LocalhostTunnel.Desktop.Tests.ViewModels;

public sealed class ConfigurationViewModelTests
{
    [Fact]
    public async Task SaveAsync_Rejects_Invalid_Config_And_Exposes_FieldErrors()
    {
        var profile = CreateStandardProfile() with
        {
            TunnelUrl = "https://demo.trycloudflare.com/",
            TunnelToken = "token"
        };
        var runtimeManager = CreateRuntimeManager(profile);
        await runtimeManager.LoadAsync(CancellationToken.None);

        var vm = new ConfigurationViewModel(runtimeManager);
        await vm.LoadAsync();
        vm.TunnelToken = "";
        vm.TargetPort = 0;

        await vm.SaveAsync();

        vm.FieldErrors.Should().ContainKey(nameof(vm.TunnelToken));
        vm.FieldErrors.Should().ContainKey(nameof(vm.TargetPort));
    }

    [Fact]
    public async Task SaveAsync_Valid_Config_Persists_Profile_Values()
    {
        var profile = CreateStandardProfile();
        var runtimeManager = CreateRuntimeManager(profile);
        await runtimeManager.LoadAsync(CancellationToken.None);
        var vm = new ConfigurationViewModel(runtimeManager);
        await vm.LoadAsync();

        vm.TunnelUrl = "https://proxypal.wigdealer.net";
        vm.TunnelToken = "token-123";
        vm.TargetPort = 8765;
        vm.Port = 8788;
        vm.Host = "127.0.0.1";
        vm.TargetHost = "127.0.0.1";
        vm.TargetProtocol = "http";

        await vm.SaveAsync();

        var saved = runtimeManager.Profiles.Single();
        saved.TunnelUrl.Should().Be("https://proxypal.wigdealer.net");
        saved.TunnelToken.Should().Be("token-123");
        vm.FieldErrors.Should().BeEmpty();
    }

    private static RuntimeManager CreateRuntimeManager(TunnelProfile profile)
    {
        var profilesStore = new InMemoryProfilesConfigStore(new ProfilesConfig
        {
            SelectedProfileId = profile.Id,
            Profiles = [profile]
        });

        return new RuntimeManager(
            profilesStore,
            new FakeTunnelHostFactory(),
            new FakeForwarderHostFactory(),
            new FakeTavilyProxyHostFactory(),
            new ObservableLogStore());
    }

    private static TunnelProfile CreateStandardProfile()
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
            Port = 8788,
            TargetProtocol = "http"
        };
    }

    private sealed class InMemoryProfilesConfigStore : IProfilesConfigStore
    {
        public InMemoryProfilesConfigStore(ProfilesConfig config)
        {
            Current = config;
        }

        public ProfilesConfig Current { get; private set; }

        public Task<ProfilesConfig> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Current);
        }

        public Task SaveAsync(ProfilesConfig config, CancellationToken cancellationToken = default)
        {
            Current = config;
            return Task.CompletedTask;
        }
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

    private sealed class FakeForwarderHost : IForwarderHost
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

    private sealed class FakeTavilyHost : ITavilyProxyHost
    {
        public ServiceState CurrentState { get; private set; } = ServiceState.Stopped;

        public Task StartAsync(TavilyConfig config, CancellationToken cancellationToken)
        {
            CurrentState = ServiceState.Running;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            CurrentState = ServiceState.Stopped;
            return Task.CompletedTask;
        }
    }
}

