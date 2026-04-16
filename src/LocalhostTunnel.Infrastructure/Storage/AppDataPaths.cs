namespace LocalhostTunnel.Infrastructure.Storage;

public sealed record AppDataPaths(
    string RootDirectory,
    string ConfigFilePath,
    string ProfilesConfigFilePath,
    string SessionFilePath,
    string LogDirectory,
    string CloudflaredDirectory)
{
    public static AppDataPaths Build()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalhostTunnel");

        return new(
            root,
            Path.Combine(root, "config.json"),
            Path.Combine(root, "config.profiles.json"),
            Path.Combine(root, "session.json"),
            Path.Combine(root, "logs"),
            Path.Combine(root, "cloudflared"));
    }
}
