using FluentAssertions;
using LocalhostTunnel.Core.Configuration;
using LocalhostTunnel.Infrastructure.Storage;
using System.Text.Json;

namespace LocalhostTunnel.Infrastructure.Tests.Storage;

public class LegacyConfigImporterTests : IDisposable
{
    private readonly string _rootDirectory;
    private readonly string _legacyProjectRoot;
    private readonly AppDataPaths _paths;

    public LegacyConfigImporterTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "LocalhostTunnel.Tests", Guid.NewGuid().ToString("N"));
        _legacyProjectRoot = Path.Combine(_rootDirectory, "legacy");

        Directory.CreateDirectory(_rootDirectory);
        Directory.CreateDirectory(_legacyProjectRoot);

        _paths = new AppDataPaths(
            _rootDirectory,
            Path.Combine(_rootDirectory, "config.json"),
            Path.Combine(_rootDirectory, "session.json"),
            Path.Combine(_rootDirectory, "logs"),
            Path.Combine(_rootDirectory, "cloudflared"));

        var legacyConfig = new AppConfig
        {
            TunnelUrl = "https://legacy.example.trycloudflare.com/",
            TunnelToken = "legacy-token",
            TargetPort = 4444,
            Port = 5555,
            Host = "127.0.0.1",
            TargetHost = "localhost",
            TargetProtocol = "http",
            WebhookSecret = "legacy-secret",
            MaxBodySize = 2048,
            UpstreamTimeout = 1234,
            LogLevel = "warn"
        };

        File.WriteAllText(
            Path.Combine(_legacyProjectRoot, "config.json"),
            JsonSerializer.Serialize(legacyConfig, new JsonSerializerOptions { WriteIndented = true }));
    }

    [Fact]
    public async Task ImportAsync_Prefers_Legacy_Project_Config_When_New_Config_Is_Missing()
    {
        var importer = new LegacyConfigImporter(_paths, _legacyProjectRoot);

        var imported = await importer.ImportAsync(CancellationToken.None);

        imported.Should().BeTrue();
        File.Exists(_paths.ConfigFilePath).Should().BeTrue();

        var importedConfig = JsonSerializer.Deserialize<AppConfig>(
            await File.ReadAllTextAsync(_paths.ConfigFilePath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = false });

        importedConfig.Should().NotBeNull();
        importedConfig!.TunnelToken.Should().Be("legacy-token");
        importedConfig.TargetPort.Should().Be(4444);
    }

    [Fact]
    public async Task ImportAsync_Replaces_Default_New_Config_But_Preserves_User_Config()
    {
        var store = new JsonConfigStore(_paths);
        await store.SaveAsync(new AppConfig(), CancellationToken.None);

        var importer = new LegacyConfigImporter(_paths, _legacyProjectRoot);

        var imported = await importer.ImportAsync(CancellationToken.None);

        imported.Should().BeTrue();

        var importedConfig = JsonSerializer.Deserialize<AppConfig>(
            await File.ReadAllTextAsync(_paths.ConfigFilePath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = false });

        importedConfig.Should().NotBeNull();
        importedConfig!.TunnelToken.Should().Be("legacy-token");

        await store.SaveAsync(new AppConfig { TunnelUrl = "https://user.example/", TunnelToken = "user-token", TargetPort = 1234 }, CancellationToken.None);
        imported = await importer.ImportAsync(CancellationToken.None);

        imported.Should().BeFalse();

        var preservedConfig = JsonSerializer.Deserialize<AppConfig>(
            await File.ReadAllTextAsync(_paths.ConfigFilePath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = false });

        preservedConfig.Should().NotBeNull();
        preservedConfig!.TunnelToken.Should().Be("user-token");
        preservedConfig.TargetPort.Should().Be(1234);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }
}
