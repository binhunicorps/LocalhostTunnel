using FluentAssertions;
using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Core.Configuration;
using LocalhostTunnel.Infrastructure.Forwarding;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LocalhostTunnel.Infrastructure.Tests.Forwarding;

public sealed class ForwarderHostTests
{
    [Fact]
    public async Task HandleAsync_Returns_401_When_Webhook_Secret_Does_Not_Match()
    {
        await using var fixture = await ForwarderHostFixture.StartAsync(webhookSecret: "abc123");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/webhook");
        request.Headers.Add("x-webhook-secret", "wrong");
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        using var response = await fixture.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HandleAsync_Returns_413_When_Payload_Exceeds_Max_Body_Size()
    {
        await using var fixture = await ForwarderHostFixture.StartAsync(maxBodySize: 8);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/large");
        request.Content = new StringContent("this payload is too big", Encoding.UTF8, "text/plain");

        using var response = await fixture.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task HandleAsync_Forwards_Request_To_Upstream()
    {
        await using var fixture = await ForwarderHostFixture.StartAsync(webhookSecret: "abc123");
        fixture.SetUpstreamHandler(async context =>
        {
            context.Response.StatusCode = (int)HttpStatusCode.Accepted;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("upstream-ok", context.RequestAborted);
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhook?x=1");
        request.Headers.Add("x-webhook-secret", "abc123");
        request.Headers.Add("x-test-header", "hello");
        request.Content = new StringContent("{\"name\":\"demo\"}", Encoding.UTF8, "application/json");

        using var response = await fixture.Client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        responseBody.Should().Be("upstream-ok");

        fixture.LastUpstreamRequest.Should().NotBeNull();
        fixture.LastUpstreamRequest!.Method.Should().Be("POST");
        fixture.LastUpstreamRequest.PathAndQuery.Should().Be("/api/webhook?x=1");
        Encoding.UTF8.GetString(fixture.LastUpstreamRequest.Body).Should().Be("{\"name\":\"demo\"}");
        fixture.LastUpstreamRequest.Headers["x-forwarded-by"].Should().Be("webhook-forwarder");
        fixture.LastUpstreamRequest.Headers["x-test-header"].Should().Be("hello");
    }

    [Fact]
    public async Task HandleAsync_Rewrites_Localhost_Urls_In_Text_Bodies()
    {
        await using var fixture = await ForwarderHostFixture.StartAsync(tunnelUrl: "https://public.example.com/");
        fixture.SetUpstreamHandler(async context =>
        {
            var body = $"visit http://127.0.0.1:{fixture.UpstreamPort}/test";
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(body, context.RequestAborted);
        });

        var response = await fixture.Client.GetStringAsync("/rewrite");

        response.Should().Contain("https://public.example.com/test");
    }

    private sealed class ForwarderHostFixture : IAsyncDisposable
    {
        private readonly WebApplication _upstream;
        private readonly IForwarderHost _forwarderHost;
        private readonly UpstreamState _state;

        private ForwarderHostFixture(
            WebApplication upstream,
            IForwarderHost forwarderHost,
            HttpClient client,
            Uri forwarderUri,
            int upstreamPort,
            UpstreamState state)
        {
            _upstream = upstream;
            _forwarderHost = forwarderHost;
            Client = client;
            ForwarderUri = forwarderUri;
            UpstreamPort = upstreamPort;
            _state = state;
        }

        public HttpClient Client { get; }

        public Uri ForwarderUri { get; }

        public int UpstreamPort { get; }

        public ForwardedRequest? LastUpstreamRequest => _state.LastRequest;

        public static async Task<ForwarderHostFixture> StartAsync(
            string webhookSecret = "",
            int maxBodySize = 10 * 1024 * 1024,
            string tunnelUrl = "https://public.example.com/",
            int upstreamTimeout = 30000)
        {
            var upstreamPort = GetFreePort();
            var forwarderPort = GetFreePort();

            var state = new UpstreamState();
            state.Handler = async context =>
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("ok", context.RequestAborted);
            };

            var upstreamBuilder = WebApplication.CreateSlimBuilder();
            upstreamBuilder.WebHost.UseUrls($"http://127.0.0.1:{upstreamPort}");
            var upstream = upstreamBuilder.Build();

            upstream.Map("/{**path}", async context =>
            {
                using var reader = new MemoryStream();
                await context.Request.Body.CopyToAsync(reader, context.RequestAborted);
                state.LastRequest = new ForwardedRequest(
                    context.Request.Method,
                    $"{context.Request.Path}{context.Request.QueryString}",
                    reader.ToArray(),
                    context.Request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.OrdinalIgnoreCase));

                await state.Handler(context);
            });

            await upstream.StartAsync();

            var config = new AppConfig
            {
                TunnelUrl = tunnelUrl,
                TunnelToken = "token-123",
                Port = forwarderPort,
                Host = "127.0.0.1",
                TargetHost = "127.0.0.1",
                TargetPort = upstreamPort,
                TargetProtocol = "http",
                WebhookSecret = webhookSecret,
                MaxBodySize = maxBodySize,
                UpstreamTimeout = upstreamTimeout
            };

            IForwarderHost forwarderHost = new ForwarderHost();
            await forwarderHost.StartAsync(config, CancellationToken.None);

            var client = new HttpClient
            {
                BaseAddress = new Uri($"http://127.0.0.1:{forwarderPort}")
            };

            return new ForwarderHostFixture(
                upstream,
                forwarderHost,
                client,
                new Uri($"http://127.0.0.1:{forwarderPort}/"),
                upstreamPort,
                state);
        }

        public void SetUpstreamHandler(Func<HttpContext, Task> handler)
        {
            _state.Handler = handler;
        }

        public async ValueTask DisposeAsync()
        {
            await _forwarderHost.StopAsync(CancellationToken.None);
            await _upstream.StopAsync();
            await _upstream.DisposeAsync();
            Client.Dispose();
        }

        private static int GetFreePort()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private sealed class UpstreamState
        {
            public ForwardedRequest? LastRequest { get; set; }

            public Func<HttpContext, Task> Handler { get; set; } = _ => Task.CompletedTask;
        }
    }

    private sealed record ForwardedRequest(
        string Method,
        string PathAndQuery,
        byte[] Body,
        IReadOnlyDictionary<string, string> Headers);
}
