using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Core.Updates;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

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
        _releaseUrl = releaseUrl ?? "https://api.github.com/repos/localhosttunnel/localhosttunnel-desktop/releases/latest";
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
        if (payload is null || string.IsNullOrWhiteSpace(payload.TagName) || string.IsNullOrWhiteSpace(payload.ZipballUrl))
        {
            return null;
        }

        var tagVersion = payload.TagName.Trim().TrimStart('v', 'V');
        if (!Version.TryParse(tagVersion, out var candidateVersion))
        {
            return null;
        }

        return candidateVersion > _currentVersion
            ? new ReleaseInfo(tagVersion, payload.ZipballUrl, payload.Body)
            : null;
    }

    private sealed class GitHubReleaseDto
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("zipball_url")]
        public string ZipballUrl { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string? Body { get; set; }
    }
}
