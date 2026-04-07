using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Core.Configuration;
using System.Text.Json;

namespace LocalhostTunnel.Infrastructure.Storage;

public sealed class JsonConfigStore : IConfigStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly AppDataPaths _paths;

    public JsonConfigStore(AppDataPaths paths)
    {
        _paths = paths;
    }

    public async Task<AppConfig> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_paths.ConfigFilePath))
        {
            var defaults = new AppConfig();
            await SaveAsync(defaults, cancellationToken);
            return defaults;
        }

        try
        {
            await using var stream = File.OpenRead(_paths.ConfigFilePath);
            var config = await JsonSerializer.DeserializeAsync<AppConfig>(stream, SerializerOptions, cancellationToken);

            if (config is not null)
            {
                return config;
            }
        }
        catch (JsonException)
        {
            return new AppConfig();
        }
        catch (IOException)
        {
            return new AppConfig();
        }

        return new AppConfig();
    }

    public async Task SaveAsync(AppConfig config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        await WriteAtomicAsync(_paths.ConfigFilePath, config, cancellationToken);
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
