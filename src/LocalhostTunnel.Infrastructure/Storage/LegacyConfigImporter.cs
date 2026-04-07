using LocalhostTunnel.Core.Configuration;
using System.Text.Json;

namespace LocalhostTunnel.Infrastructure.Storage;

public sealed class LegacyConfigImporter
{
    private readonly AppDataPaths _paths;
    private readonly string _legacyProjectRoot;

    public LegacyConfigImporter(AppDataPaths paths, string legacyProjectRoot)
    {
        _paths = paths;
        _legacyProjectRoot = legacyProjectRoot;
    }

    public async Task<bool> ImportAsync(CancellationToken cancellationToken = default)
    {
        var legacyConfigPath = Path.Combine(_legacyProjectRoot, "config.json");
        if (!File.Exists(legacyConfigPath))
        {
            return false;
        }

        if (File.Exists(_paths.ConfigFilePath))
        {
            var currentConfig = await TryReadConfigAsync(_paths.ConfigFilePath, cancellationToken);
            if (currentConfig is null || !IsDefaultConfig(currentConfig))
            {
                return false;
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_paths.ConfigFilePath)!);

        await using var source = File.OpenRead(legacyConfigPath);
        var tempPath = Path.Combine(
            Path.GetDirectoryName(_paths.ConfigFilePath)!,
            $"{Path.GetFileName(_paths.ConfigFilePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var destination = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await source.CopyToAsync(destination, cancellationToken);
                await destination.FlushAsync(cancellationToken);
            }

            if (File.Exists(_paths.ConfigFilePath))
            {
                File.Replace(tempPath, _paths.ConfigFilePath, destinationBackupFileName: null);
            }
            else
            {
                File.Move(tempPath, _paths.ConfigFilePath);
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }

        return true;
    }

    private static async Task<AppConfig?> TryReadConfigAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<AppConfig>(stream, cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static bool IsDefaultConfig(AppConfig config)
    {
        var defaults = new AppConfig();

        return config.TunnelUrl == defaults.TunnelUrl
            && config.TunnelToken == defaults.TunnelToken
            && config.TargetPort == defaults.TargetPort
            && config.Port == defaults.Port
            && config.Host == defaults.Host
            && config.TargetHost == defaults.TargetHost
            && config.TargetProtocol == defaults.TargetProtocol
            && config.WebhookSecret == defaults.WebhookSecret
            && config.MaxBodySize == defaults.MaxBodySize
            && config.UpstreamTimeout == defaults.UpstreamTimeout
            && config.LogLevel == defaults.LogLevel;
    }
}
