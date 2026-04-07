using FluentAssertions;
using LocalhostTunnel.Core.Configuration;
using LocalhostTunnel.Infrastructure.Storage;
using System.Text.Json;

namespace LocalhostTunnel.Infrastructure.Tests.Storage;

public class JsonConfigStoreTests : IDisposable
{
    private readonly string _rootDirectory;
    private readonly AppDataPaths _paths;

    public JsonConfigStoreTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "LocalhostTunnel.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootDirectory);

        _paths = new AppDataPaths(
            _rootDirectory,
            Path.Combine(_rootDirectory, "config.json"),
            Path.Combine(_rootDirectory, "session.json"),
            Path.Combine(_rootDirectory, "logs"),
            Path.Combine(_rootDirectory, "cloudflared"));
    }

    [Fact]
    public async Task SaveAsync_Then_LoadAsync_RoundTrips_The_Config()
    {
        var store = new JsonConfigStore(_paths);
        var expected = new AppConfig
        {
            TunnelUrl = "https://example.trycloudflare.com/",
            TunnelToken = "token-123",
            TargetPort = 4321,
            Port = 9876,
            Host = "0.0.0.0",
            TargetHost = "localhost",
            TargetProtocol = "https",
            WebhookSecret = "secret",
            MaxBodySize = 1024,
            UpstreamTimeout = 5000,
            LogLevel = "debug"
        };

        await store.SaveAsync(expected, CancellationToken.None);
        var loaded = await store.LoadAsync(CancellationToken.None);

        loaded.Should().BeEquivalentTo(expected);
        File.Exists(_paths.ConfigFilePath).Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_On_Corrupt_Config_Returns_Defaults_Without_Overwriting_Existing_File()
    {
        await File.WriteAllTextAsync(_paths.ConfigFilePath, "{ not valid json");
        var original = await File.ReadAllTextAsync(_paths.ConfigFilePath);
        var store = new JsonConfigStore(_paths);

        var loaded = await store.LoadAsync(CancellationToken.None);
        var after = await File.ReadAllTextAsync(_paths.ConfigFilePath);

        loaded.Should().BeEquivalentTo(new AppConfig());
        after.Should().Be(original);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }
}
