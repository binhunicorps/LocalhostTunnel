using FluentAssertions;
using LocalhostTunnel.Infrastructure.Storage;
using System.IO;

namespace LocalhostTunnel.Core.Tests.Configuration;

public class AppDataPathsTests
{
    [Fact]
    public void Build_Uses_LocalAppData_And_LocalhostTunnel_Subfolder()
    {
        var paths = AppDataPaths.Build();
        var localApplicationData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalhostTunnel");

        paths.RootDirectory.Should().Be(localApplicationData);
        paths.ConfigFilePath.Should().Be(Path.Combine(localApplicationData, "config.json"));
        paths.SessionFilePath.Should().Be(Path.Combine(localApplicationData, "session.json"));
        paths.LogDirectory.Should().Be(Path.Combine(localApplicationData, "logs"));
        paths.CloudflaredDirectory.Should().Be(Path.Combine(localApplicationData, "cloudflared"));
    }
}
