using System.Text;

namespace LocalhostTunnel.Infrastructure.Forwarding;

internal sealed class LocalUrlRewriter
{
    private static readonly string[] TextMediaTypeMarkers =
    [
        "json",
        "text",
        "html",
        "xml",
        "javascript",
        "css",
        "urlencoded"
    ];

    public byte[] Rewrite(
        byte[] body,
        string? mediaType,
        string tunnelUrl,
        string targetHost,
        int targetPort)
    {
        if (body.Length == 0 || string.IsNullOrWhiteSpace(tunnelUrl) || !IsTextMediaType(mediaType))
        {
            return body;
        }

        var normalizedTunnelUrl = tunnelUrl.TrimEnd('/');
        if (normalizedTunnelUrl.Length == 0)
        {
            return body;
        }

        var original = Encoding.UTF8.GetString(body);
        var rewritten = original;

        var hosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "127.0.0.1",
            "localhost"
        };

        if (!string.IsNullOrWhiteSpace(targetHost))
        {
            hosts.Add(targetHost);
        }

        foreach (var host in hosts)
        {
            rewritten = rewritten.Replace($"http://{host}:{targetPort}", normalizedTunnelUrl, StringComparison.OrdinalIgnoreCase);
            rewritten = rewritten.Replace($"https://{host}:{targetPort}", normalizedTunnelUrl, StringComparison.OrdinalIgnoreCase);
        }

        if (ReferenceEquals(original, rewritten) || string.Equals(original, rewritten, StringComparison.Ordinal))
        {
            return body;
        }

        return Encoding.UTF8.GetBytes(rewritten);
    }

    private static bool IsTextMediaType(string? mediaType)
    {
        if (string.IsNullOrWhiteSpace(mediaType))
        {
            return false;
        }

        return TextMediaTypeMarkers.Any(marker => mediaType.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
