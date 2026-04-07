using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Core.Configuration;
using System.Text.Json;

namespace LocalhostTunnel.Infrastructure.Storage;

public sealed class JsonSessionStore : ISessionStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly AppDataPaths _paths;

    public JsonSessionStore(AppDataPaths paths)
    {
        _paths = paths;
    }

    public async Task<SessionState> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_paths.SessionFilePath))
        {
            var defaults = new SessionState();
            await SaveAsync(defaults, cancellationToken);
            return defaults;
        }

        try
        {
            await using var stream = File.OpenRead(_paths.SessionFilePath);
            var sessionState = await JsonSerializer.DeserializeAsync<SessionState>(stream, SerializerOptions, cancellationToken);

            if (sessionState is not null)
            {
                return sessionState;
            }
        }
        catch (JsonException)
        {
            return new SessionState();
        }
        catch (IOException)
        {
            return new SessionState();
        }

        return new SessionState();
    }

    public async Task SaveAsync(SessionState sessionState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sessionState);

        await WriteAtomicAsync(_paths.SessionFilePath, sessionState, cancellationToken);
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
