using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace LocalhostTunnel.Infrastructure.Forwarding;

internal static class HeaderSanitizer
{
    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "host",
        "connection",
        "keep-alive",
        "proxy-authenticate",
        "proxy-authorization",
        "te",
        "trailer",
        "transfer-encoding",
        "upgrade",
        "content-length"
    };

    public static void CopySafeRequestHeaders(
        IHeaderDictionary sourceHeaders,
        HttpRequestMessage upstreamRequest,
        string requestId)
    {
        foreach (var pair in sourceHeaders)
        {
            if (HopByHopHeaders.Contains(pair.Key))
            {
                continue;
            }

            var values = pair.Value.ToArray();
            if (values.Length == 0)
            {
                continue;
            }

            if (!upstreamRequest.Headers.TryAddWithoutValidation(pair.Key, values) &&
                upstreamRequest.Content is not null)
            {
                upstreamRequest.Content.Headers.TryAddWithoutValidation(pair.Key, values);
            }
        }

        sourceHeaders.TryGetValue("host", out var hostValues);
        sourceHeaders.TryGetValue("x-forwarded-proto", out var forwardedProtoValues);
        var forwardedProto = forwardedProtoValues.ToString();

        SetSingleHeader(upstreamRequest.Headers, "x-forwarded-by", "webhook-forwarder");
        SetSingleHeader(upstreamRequest.Headers, "x-forwarded-host", hostValues.ToString());
        SetSingleHeader(upstreamRequest.Headers, "x-forwarded-proto", string.IsNullOrWhiteSpace(forwardedProto) ? "https" : forwardedProto);
        SetSingleHeader(upstreamRequest.Headers, "x-request-id", requestId);
    }

    public static IReadOnlyDictionary<string, string[]> CopySafeResponseHeaders(
        HttpResponseHeaders responseHeaders,
        HttpContentHeaders contentHeaders,
        int contentLength)
    {
        var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in responseHeaders)
        {
            if (HopByHopHeaders.Contains(pair.Key))
            {
                continue;
            }

            result[pair.Key] = pair.Value.ToArray();
        }

        foreach (var pair in contentHeaders)
        {
            if (HopByHopHeaders.Contains(pair.Key))
            {
                continue;
            }

            result[pair.Key] = pair.Value.ToArray();
        }

        result["content-length"] = [contentLength.ToString(System.Globalization.CultureInfo.InvariantCulture)];
        return result;
    }

    private static void SetSingleHeader(HttpRequestHeaders headers, string key, string value)
    {
        headers.Remove(key);
        headers.TryAddWithoutValidation(key, value);
    }
}
