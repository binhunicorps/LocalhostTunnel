using System.Diagnostics;
using System.IO.Compression;

namespace LocalhostTunnel.Updater;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var parsed = ParseArguments(args);
        if (!parsed.TryGetValue("downloadUrl", out var downloadUrl) ||
            !parsed.TryGetValue("targetDir", out var targetDir) ||
            !parsed.TryGetValue("restartExe", out var restartExe))
        {
            Console.Error.WriteLine("Required args: --downloadUrl --targetDir --restartExe");
            return 1;
        }

        var tempRoot = Path.Combine(Path.GetTempPath(), "LocalhostTunnel.Updater", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var archivePath = Path.Combine(tempRoot, "release.zip");
        var extractPath = Path.Combine(tempRoot, "extract");

        try
        {
            using (var httpClient = new HttpClient())
            await using (var archive = File.Create(archivePath))
            {
                await using var stream = await httpClient.GetStreamAsync(downloadUrl);
                await stream.CopyToAsync(archive);
            }

            ZipFile.ExtractToDirectory(archivePath, extractPath, overwriteFiles: true);
            var sourceRoot = ResolveSourceRoot(extractPath);
            CopyDirectory(sourceRoot, targetDir);

            var restartPath = Path.Combine(targetDir, restartExe);
            if (File.Exists(restartPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = restartPath,
                    WorkingDirectory = targetDir,
                    UseShellExecute = true
                });
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                try
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
                catch
                {
                }
            }
        }
    }

    private static Dictionary<string, string> ParseArguments(string[] args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length - 1; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            result[args[i][2..]] = args[i + 1];
            i++;
        }

        return result;
    }

    private static string ResolveSourceRoot(string extractPath)
    {
        var directories = Directory.GetDirectories(extractPath);
        return directories.Length == 1 ? directories[0] : extractPath;
    }

    private static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);

        foreach (var filePath in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, filePath);
            var targetFilePath = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFilePath)!);
            File.Copy(filePath, targetFilePath, overwrite: true);
        }
    }
}
