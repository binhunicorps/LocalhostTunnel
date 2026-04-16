using FluentAssertions;
using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Core.Runtime;
using LocalhostTunnel.Infrastructure.Logging;
using LocalhostTunnel.Infrastructure.Storage;
using LocalhostTunnel.Infrastructure.Tunnel;

namespace LocalhostTunnel.Infrastructure.Tests.Tunnel;

public sealed class CloudflaredProcessHostTests : IDisposable
{
    private readonly string _rootDirectory;
    private readonly AppDataPaths _paths;

    public CloudflaredProcessHostTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "LocalhostTunnel.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootDirectory);

        _paths = new AppDataPaths(
            _rootDirectory,
            Path.Combine(_rootDirectory, "config.json"),
            Path.Combine(_rootDirectory, "config.profiles.json"),
            Path.Combine(_rootDirectory, "session.json"),
            Path.Combine(_rootDirectory, "logs"),
            Path.Combine(_rootDirectory, "cloudflared"));
    }

    [Fact]
    public async Task StartAsync_Requires_Token_And_Leaves_State_Stopped()
    {
        var installer = new FakeInstaller(_paths, createExecutable: false);
        var host = new CloudflaredProcessHost(installer, new ObservableLogStore(), _paths);

        var result = await host.StartAsync("", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        host.Current.State.Should().Be(ServiceState.Stopped);
        installer.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task StartAsync_Returns_Failure_When_Installer_Does_Not_Provide_Executable()
    {
        var installer = new FakeInstaller(_paths, createExecutable: false);
        var host = new CloudflaredProcessHost(installer, new ObservableLogStore(), _paths);

        var result = await host.StartAsync("token-123", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        host.Current.State.Should().Be(ServiceState.Faulted);
        installer.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task StopAsync_When_Not_Running_Leaves_State_Stopped()
    {
        var installer = new FakeInstaller(_paths, createExecutable: false);
        var host = new CloudflaredProcessHost(installer, new ObservableLogStore(), _paths);

        await host.StopAsync(CancellationToken.None);

        host.Current.State.Should().Be(ServiceState.Stopped);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }

    private sealed class FakeInstaller : ICloudflaredInstaller
    {
        private readonly AppDataPaths _paths;
        private readonly bool _createExecutable;

        public FakeInstaller(AppDataPaths paths, bool createExecutable)
        {
            _paths = paths;
            _createExecutable = createExecutable;
        }

        public int CallCount { get; private set; }

        public Task EnsureInstalledAsync(CancellationToken cancellationToken)
        {
            CallCount++;
            Directory.CreateDirectory(_paths.CloudflaredDirectory);

            if (_createExecutable)
            {
                File.WriteAllText(Path.Combine(_paths.CloudflaredDirectory, "cloudflared.exe"), "fake");
            }

            return Task.CompletedTask;
        }
    }
}
