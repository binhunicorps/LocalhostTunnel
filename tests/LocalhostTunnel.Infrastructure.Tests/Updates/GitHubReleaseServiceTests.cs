using FluentAssertions;
using LocalhostTunnel.Infrastructure.Updates;
using System.Net;
using System.Net.Http;
using System.Text;

namespace LocalhostTunnel.Infrastructure.Tests.Updates;

public sealed class GitHubReleaseServiceTests
{
    [Fact]
    public async Task CheckForUpdatesAsync_Returns_Latest_Release_When_Version_Is_Newer()
    {
        var payload = """
        {
          "tag_name": "v1.0.16",
          "zipball_url": "https://example.com/download.zip",
          "body": "Bug fixes"
        }
        """;

        var httpClient = new HttpClient(new StubHandler(payload));
        var service = new GitHubReleaseService(httpClient, currentVersion: "1.0.15");

        var release = await service.CheckForUpdatesAsync(CancellationToken.None);

        release.Should().NotBeNull();
        release!.Version.Should().Be("1.0.16");
    }

    [Fact]
    public async Task CheckForUpdatesAsync_Returns_Null_When_Release_Is_Not_Newer()
    {
        var payload = """
        {
          "tag_name": "v1.0.15",
          "zipball_url": "https://example.com/download.zip",
          "body": "Current"
        }
        """;

        var httpClient = new HttpClient(new StubHandler(payload));
        var service = new GitHubReleaseService(httpClient, currentVersion: "1.0.15");

        var release = await service.CheckForUpdatesAsync(CancellationToken.None);

        release.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdatesAsync_Returns_Null_When_Release_Endpoint_Is_NotFound()
    {
        var httpClient = new HttpClient(new StubHandler("{}", HttpStatusCode.NotFound));
        var service = new GitHubReleaseService(httpClient, currentVersion: "1.0.15");

        var release = await service.CheckForUpdatesAsync(CancellationToken.None);

        release.Should().BeNull();
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _payload;
        private readonly HttpStatusCode _statusCode;

        public StubHandler(string payload, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _payload = payload;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_payload, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }
}
