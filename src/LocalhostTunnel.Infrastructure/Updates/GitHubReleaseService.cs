using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Core.Updates;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Linq;

namespace LocalhostTunnel.Infrastructure.Updates;

public sealed class GitHubReleaseService : IUpdateService
{
    private const string EmbeddedGitHubToken = "";

    private readonly HttpClient _httpClient;
    private readonly Version _currentVersion;
    private readonly string _releaseUrl;

    public GitHubReleaseService(
        HttpClient httpClient,
        string currentVersion,
        string? releaseUrl = null)
    {
        _httpClient = httpClient;
        _currentVersion = Version.Parse(currentVersion);
        _releaseUrl = releaseUrl ?? "https://api.github.com/repos/binhunicorps/LocalhostTunnel/releases/latest";
    }

    public async Task<ReleaseInfo?> CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _releaseUrl);
        request.Headers.UserAgent.ParseAdd("LocalhostTunnel.Desktop/1.0");
        if (!string.IsNullOrWhiteSpace(EmbeddedGitHubToken))
        {
            request.Headers.Authorization = new("Bearer", EmbeddedGitHubToken);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<GitHubReleaseDto>(cancellationToken: cancellationToken);
        if (payload is null || string.IsNullOrWhiteSpace(payload.TagName))
        {
            return null;
        }

        var downloadUrl = SelectDownloadUrl(payload);
        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            return null;
        }

        var tagVersion = payload.TagName.Trim().TrimStart('v', 'V');
        if (!Version.TryParse(tagVersion, out var candidateVersion))
        {
            return null;
        }

        return candidateVersion > _currentVersion
            ? new ReleaseInfo(tagVersion, downloadUrl, payload.Body)
            : null;
    }

    private static string? SelectDownloadUrl(GitHubReleaseDto payload)
    {
        if (payload.Assets is not null)
        {
            var preferredAsset = payload.Assets.FirstOrDefault(static asset =>
                !string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl) &&
                asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                asset.Name.Contains("portable-win-x64", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(preferredAsset?.BrowserDownloadUrl))
            {
                return preferredAsset.BrowserDownloadUrl;
            }

            var firstZipAsset = payload.Assets.FirstOrDefault(static asset =>
                !string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl) &&
                asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(firstZipAsset?.BrowserDownloadUrl))
            {
                return firstZipAsset.BrowserDownloadUrl;
            }
        }

        return payload.ZipballUrl;
    }

    private sealed class GitHubReleaseDto
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("zipball_url")]
        public string ZipballUrl { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubReleaseAssetDto>? Assets { get; set; }
    }

    private sealed class GitHubReleaseAssetDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}
