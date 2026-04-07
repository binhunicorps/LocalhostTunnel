using FluentAssertions;
using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Application.Services;
using LocalhostTunnel.Core.Configuration;
using LocalhostTunnel.Core.Runtime;
using LocalhostTunnel.Infrastructure.Logging;

namespace LocalhostTunnel.Infrastructure.Tests;

public sealed class RuntimeCoordinatorTests
{
    [Fact]
    public async Task StartAsync_Starts_Tunnel_Before_Forwarder()
    {
        var counter = new InvocationCounter();
        var tunnelHost = new FakeTunnelHost(counter);
        var forwarderHost = new FakeForwarderHost(counter);
        var configStore = new FakeConfigStore(CreateValidConfig());
        var coordinator = new RuntimeCoordinator(
            tunnelHost,
            forwarderHost,
            configStore,
            new ObservableLogStore());

        await coordinator.StartAsync(CancellationToken.None);

        tunnelHost.CallOrder.Should().BeLessThan(forwarderHost.CallOrder);
    }

    [Fact]
    public async Task StartAsync_Throws_When_Config_Is_Invalid()
    {
        var counter = new InvocationCounter();
        var tunnelHost = new FakeTunnelHost(counter);
        var forwarderHost = new FakeForwarderHost(counter);
        var configStore = new FakeConfigStore(new AppConfig());
        var coordinator = new RuntimeCoordinator(
            tunnelHost,
            forwarderHost,
            configStore,
            new ObservableLogStore());

        var action = async () => await coordinator.StartAsync(CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        tunnelHost.CallOrder.Should().Be(0);
        forwarderHost.CallOrder.Should().Be(0);
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

    private sealed class FakeConfigStore : IConfigStore
    {
        private readonly AppConfig _config;

        public FakeConfigStore(AppConfig config)
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

    private sealed class FakeTunnelHost : ITunnelHost
    {
        private readonly InvocationCounter _counter;

        public FakeTunnelHost(InvocationCounter counter)
        {
            _counter = counter;
        }

        public int CallOrder { get; private set; }

        public TunnelSnapshot Current { get; private set; } = new();

        public Task<TunnelStartResult> StartAsync(string tunnelToken, CancellationToken cancellationToken)
        {
            CallOrder = _counter.Next();
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
        private readonly InvocationCounter _counter;

        public FakeForwarderHost(InvocationCounter counter)
        {
            _counter = counter;
        }

        public int CallOrder { get; private set; }

        public ForwarderSnapshot Current { get; private set; } = new();

        public Task StartAsync(AppConfig config, CancellationToken cancellationToken)
        {
            CallOrder = _counter.Next();
            Current = ForwarderSnapshot.CreateRunning(DateTimeOffset.UtcNow, 1234);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Current = new ForwarderSnapshot();
            return Task.CompletedTask;
        }
    }

    private sealed class InvocationCounter
    {
        private int _value;

        public int Next()
        {
            _value++;
            return _value;
        }
    }
}
