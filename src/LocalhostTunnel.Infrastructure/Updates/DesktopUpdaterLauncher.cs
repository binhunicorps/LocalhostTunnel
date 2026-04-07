using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Core.Updates;
using System.Diagnostics;

namespace LocalhostTunnel.Infrastructure.Updates;

public sealed class DesktopUpdaterLauncher : IUpdaterLauncher
{
    private readonly string _updaterExecutablePath;
    private readonly string _targetDirectory;
    private readonly string _desktopExecutableName;

    public DesktopUpdaterLauncher()
        : this(
            Path.Combine(AppContext.BaseDirectory, "LocalhostTunnel.Updater.exe"),
            AppContext.BaseDirectory,
            "LocalhostTunnel.Desktop.exe")
    {
    }

    internal DesktopUpdaterLauncher(string updaterExecutablePath, string targetDirectory, string desktopExecutableName)
    {
        _updaterExecutablePath = updaterExecutablePath;
        _targetDirectory = targetDirectory;
        _desktopExecutableName = desktopExecutableName;
    }

    public Task LaunchAsync(ReleaseInfo release, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(_updaterExecutablePath))
        {
            throw new FileNotFoundException("Updater executable not found.", _updaterExecutablePath);
        }

        var arguments =
            $"--downloadUrl \"{release.DownloadUrl}\" " +
            $"--targetDir \"{_targetDirectory}\" " +
            $"--restartExe \"{_desktopExecutableName}\" " +
            $"--waitPid \"{Environment.ProcessId}\"";

        var startInfo = new ProcessStartInfo
        {
            FileName = _updaterExecutablePath,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process.Start(startInfo);
        return Task.CompletedTask;
    }
}
