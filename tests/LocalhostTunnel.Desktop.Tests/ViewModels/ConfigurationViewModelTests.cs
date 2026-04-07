using FluentAssertions;
using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Application.Services;
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
        var configStore = new InMemoryConfigStore(new AppConfig
        {
            TunnelUrl = "https://demo.trycloudflare.com/",
            TunnelToken = "token"
        });

        var runtimeCoordinator = new RuntimeCoordinator(
            new FakeTunnelHost(),
            new FakeForwarderHost(),
            configStore,
            new ObservableLogStore());

        var vm = new ConfigurationViewModel(configStore, runtimeCoordinator)
        {
            TunnelUrl = "https://demo.trycloudflare.com/",
            TunnelToken = "",
            TargetPort = 0
        };

        await vm.SaveAsync();

        vm.FieldErrors.Should().ContainKey(nameof(vm.TunnelToken));
        vm.FieldErrors.Should().ContainKey(nameof(vm.TargetPort));
    }

    [Fact]
    public async Task SaveAsync_Valid_Config_Does_Not_Reload_Runtime_When_Runtime_Is_Stopped()
    {
        var configStore = new InMemoryConfigStore(new AppConfig
        {
            TunnelUrl = "https://demo.trycloudflare.com/",
            TunnelToken = "token"
        });

        var tunnelHost = new FakeTunnelHost();
        var forwarderHost = new FakeForwarderHost();
        var runtimeCoordinator = new RuntimeCoordinator(
            tunnelHost,
            forwarderHost,
            configStore,
            new ObservableLogStore());

        var vm = new ConfigurationViewModel(configStore, runtimeCoordinator)
        {
            TunnelUrl = "https://proxypal.wigdealer.net",
            TunnelToken = "token-123",
            TargetPort = 8765,
            Port = 8788,
            Host = "127.0.0.1",
            TargetHost = "127.0.0.1",
            TargetProtocol = "http"
        };

        await vm.SaveAsync();

        tunnelHost.StartCalls.Should().Be(0);
        forwarderHost.StartCalls.Should().Be(0);
        tunnelHost.StopCalls.Should().Be(0);
        forwarderHost.StopCalls.Should().Be(0);
        vm.FieldErrors.Should().BeEmpty();

        var savedConfig = await configStore.LoadAsync();
        savedConfig.TunnelUrl.Should().Be("https://proxypal.wigdealer.net");
        savedConfig.TunnelToken.Should().Be("token-123");
    }

    private sealed class InMemoryConfigStore : IConfigStore
    {
        public InMemoryConfigStore(AppConfig config)
        {
            Current = config;
        }

        public AppConfig Current { get; private set; }

        public Task<AppConfig> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Current);
        }

        public Task SaveAsync(AppConfig config, CancellationToken cancellationToken = default)
        {
            Current = config;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTunnelHost : ITunnelHost
    {
        public int StartCalls { get; private set; }

        public int StopCalls { get; private set; }

        public TunnelSnapshot Current { get; private set; } = new();

        public Task<TunnelStartResult> StartAsync(string tunnelToken, CancellationToken cancellationToken)
        {
            StartCalls++;
            Current = TunnelSnapshot.CreateRunning(DateTimeOffset.UtcNow);
            return Task.FromResult(new TunnelStartResult(true));
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopCalls++;
            Current = new TunnelSnapshot();
            return Task.CompletedTask;
        }
    }

    private sealed class FakeForwarderHost : IForwarderHost
    {
        public int StartCalls { get; private set; }

        public int StopCalls { get; private set; }

        public ForwarderSnapshot Current { get; private set; } = new();

        public Task StartAsync(AppConfig config, CancellationToken cancellationToken)
        {
            StartCalls++;
            Current = ForwarderSnapshot.CreateRunning(DateTimeOffset.UtcNow, 1234);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopCalls++;
            Current = new ForwarderSnapshot();
            return Task.CompletedTask;
        }
    }
}
