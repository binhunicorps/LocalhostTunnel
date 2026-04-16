using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Core.Configuration;
using System.Text.Json;

namespace LocalhostTunnel.Infrastructure.Storage;

public sealed class JsonProfilesConfigStore : IProfilesConfigStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly AppDataPaths _paths;

    public JsonProfilesConfigStore(AppDataPaths paths)
    {
        _paths = paths;
    }

    public async Task<ProfilesConfig> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(_paths.ProfilesConfigFilePath))
        {
            try
            {
                await using var profilesStream = File.OpenRead(_paths.ProfilesConfigFilePath);
                var existing = await JsonSerializer.DeserializeAsync<ProfilesConfig>(profilesStream, SerializerOptions, cancellationToken);
                if (existing is not null && existing.Profiles.Count > 0)
                {
                    return existing;
                }
            }
            catch (JsonException)
            {
            }
            catch (IOException)
            {
            }
        }

        var migrated = await TryMigrateFromLegacyConfigAsync(cancellationToken);
        await SaveAsync(migrated, cancellationToken);
        return migrated;
    }

    public async Task SaveAsync(ProfilesConfig config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        await WriteAtomicAsync(_paths.ProfilesConfigFilePath, config, cancellationToken);
    }

    private async Task<ProfilesConfig> TryMigrateFromLegacyConfigAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_paths.ConfigFilePath))
        {
            return ProfilesConfig.CreateDefault();
        }

        try
        {
            await using var stream = File.OpenRead(_paths.ConfigFilePath);
            var legacy = await JsonSerializer.DeserializeAsync<AppConfig>(stream, SerializerOptions, cancellationToken);
            if (legacy is null)
            {
                return ProfilesConfig.CreateDefault();
            }

            var standard = new TunnelProfile
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "Default Tunnel",
                Type = ProfileType.Standard,
                Enabled = true,
                TunnelUrl = legacy.TunnelUrl,
                TunnelToken = legacy.TunnelToken,
                TargetPort = legacy.TargetPort,
                Port = legacy.Port,
                Host = legacy.Host,
                TargetHost = legacy.TargetHost,
                TargetProtocol = legacy.TargetProtocol,
                WebhookSecret = legacy.WebhookSecret,
                MaxBodySize = legacy.MaxBodySize,
                UpstreamTimeout = legacy.UpstreamTimeout,
                LogLevel = legacy.LogLevel
            };

            var tavily = new TunnelProfile
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "Tavily API",
                Type = ProfileType.Tavily,
                Enabled = false,
                TargetHost = "127.0.0.1",
                TargetPort = 8766,
                Tavily = new TavilyConfig()
            };

            return new ProfilesConfig
            {
                SelectedProfileId = standard.Id,
                Profiles = new[] { standard, tavily }
            };
        }
        catch (JsonException)
        {
            return ProfilesConfig.CreateDefault();
        }
        catch (IOException)
        {
            return ProfilesConfig.CreateDefault();
        }
    }

    private static async Task WriteAtomicAsync<T>(string targetPath, T value, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(targetPath)!;
        Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(directory, $"{Path.GetFileName(targetPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await JsonSerializer.SerializeAsync(stream, value, SerializerOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            if (File.Exists(targetPath))
            {
                File.Replace(tempPath, targetPath, destinationBackupFileName: null);
            }
            else
            {
                File.Move(tempPath, targetPath);
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}

