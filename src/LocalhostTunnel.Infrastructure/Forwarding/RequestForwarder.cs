using LocalhostTunnel.Core.Configuration;
using Microsoft.AspNetCore.Http;

namespace LocalhostTunnel.Infrastructure.Forwarding;

internal sealed class RequestForwarder : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly LocalUrlRewriter _urlRewriter;
    private readonly AppConfig _config;

    public RequestForwarder(HttpClient httpClient, LocalUrlRewriter urlRewriter, AppConfig config)
    {
        _httpClient = httpClient;
        _urlRewriter = urlRewriter;
        _config = config;
    }

    public async Task<ForwarderResult> ForwardAsync(
        string method,
        string pathAndQuery,
        IHeaderDictionary requestHeaders,
        byte[] body,
        string requestId,
        CancellationToken cancellationToken)
    {
        using var upstreamRequest = new HttpRequestMessage(new HttpMethod(method), pathAndQuery);
        if (body.Length > 0)
        {
            upstreamRequest.Content = new ByteArrayContent(body);
        }

        HeaderSanitizer.CopySafeRequestHeaders(requestHeaders, upstreamRequest, requestId);

        HttpResponseMessage upstreamResponse;
        try
        {
            upstreamResponse = await _httpClient.SendAsync(
                upstreamRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new UpstreamTimeoutException(ex);
        }
        catch (HttpRequestException ex)
        {
            throw new UpstreamUnavailableException(ex);
        }

        using (upstreamResponse)
        {
            var upstreamBody = await upstreamResponse.Content.ReadAsByteArrayAsync(cancellationToken);
            var rewrittenBody = _urlRewriter.Rewrite(
                upstreamBody,
                upstreamResponse.Content.Headers.ContentType?.MediaType,
                _config.TunnelUrl,
                _config.TargetHost,
                _config.TargetPort);

            var headers = HeaderSanitizer.CopySafeResponseHeaders(
                upstreamResponse.Headers,
                upstreamResponse.Content.Headers,
                rewrittenBody.Length);

            return new ForwarderResult(
                (int)upstreamResponse.StatusCode,
                headers,
                rewrittenBody);
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

internal sealed record ForwarderResult(
    int StatusCode,
    IReadOnlyDictionary<string, string[]> Headers,
    byte[] Body);

internal sealed class UpstreamTimeoutException : Exception
{
    public UpstreamTimeoutException(Exception innerException)
        : base("Upstream request timed out.", innerException)
    {
    }
}

internal sealed class UpstreamUnavailableException : Exception
{
    public UpstreamUnavailableException(Exception innerException)
        : base("Upstream request failed.", innerException)
    {
    }
}
