using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Infrastructure.Storage;

namespace LocalhostTunnel.Infrastructure.Tunnel;

public sealed class CloudflaredInstaller : ICloudflaredInstaller, IDisposable
{
    internal const string DownloadUrl = "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe";

    private readonly HttpClient _httpClient;
    private readonly string _cloudflaredExePath;
    private readonly bool _ownsHttpClient;

    public CloudflaredInstaller(AppDataPaths paths)
        : this(new HttpClient(), paths, ownsHttpClient: true)
    {
    }

    internal CloudflaredInstaller(HttpClient httpClient, AppDataPaths paths, bool ownsHttpClient = false)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(paths);

        _httpClient = httpClient;
        _cloudflaredExePath = Path.Combine(paths.CloudflaredDirectory, "cloudflared.exe");
        _ownsHttpClient = ownsHttpClient;
    }

    public async Task EnsureInstalledAsync(CancellationToken cancellationToken)
    {
        var cloudflaredDirectory = Path.GetDirectoryName(_cloudflaredExePath);
        if (string.IsNullOrWhiteSpace(cloudflaredDirectory))
        {
            throw new InvalidOperationException("Cloudflared directory is not configured.");
        }

        Directory.CreateDirectory(cloudflaredDirectory);
        if (File.Exists(_cloudflaredExePath))
        {
            return;
        }

        using var response = await _httpClient.GetAsync(DownloadUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tempFilePath = $"{_cloudflaredExePath}.{Guid.NewGuid():N}.tmp";

        await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var destination = File.Create(tempFilePath))
        {
            await source.CopyToAsync(destination, cancellationToken);
        }

        if (File.Exists(_cloudflaredExePath))
        {
            File.Delete(tempFilePath);
            return;
        }

        File.Move(tempFilePath, _cloudflaredExePath);
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}
