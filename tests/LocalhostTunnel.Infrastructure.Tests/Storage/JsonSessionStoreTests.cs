using FluentAssertions;
using LocalhostTunnel.Core.Configuration;
using LocalhostTunnel.Infrastructure.Storage;

namespace LocalhostTunnel.Infrastructure.Tests.Storage;

public class JsonSessionStoreTests : IDisposable
{
    private readonly string _rootDirectory;
    private readonly AppDataPaths _paths;

    public JsonSessionStoreTests()
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
    public async Task SaveAsync_Then_LoadAsync_RoundTrips_The_Session_State()
    {
        var store = new JsonSessionStore(_paths);
        var expected = new SessionState
        {
            LastActiveScreen = "logs",
            WindowLeft = 25,
            WindowTop = 40,
            WindowWidth = 1440,
            WindowHeight = 900,
            LogsAutoScrollEnabled = false
        };

        await store.SaveAsync(expected, CancellationToken.None);
        var loaded = await store.LoadAsync(CancellationToken.None);

        loaded.Should().BeEquivalentTo(expected);
        File.Exists(_paths.SessionFilePath).Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_Creates_Default_Session_File_When_Missing()
    {
        var store = new JsonSessionStore(_paths);

        var loaded = await store.LoadAsync(CancellationToken.None);

        loaded.Should().BeEquivalentTo(new SessionState());
        File.Exists(_paths.SessionFilePath).Should().BeTrue();
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }
}
