using LocalhostTunnel.Core.Logging;
using System.Text;

namespace LocalhostTunnel.Infrastructure.Logging;

public sealed class FileLogWriter
{
    private readonly string _filePath;

    public FileLogWriter(string filePath)
    {
        _filePath = filePath;
    }

    public async Task AppendAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var line = $"{entry.Timestamp:O} [{entry.Level}] {entry.Source}: {entry.Message}{Environment.NewLine}";
        await File.AppendAllTextAsync(_filePath, line, Encoding.UTF8, cancellationToken);
    }
}
