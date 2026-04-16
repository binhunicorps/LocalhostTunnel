namespace LocalhostTunnel.Infrastructure.Tavily;

public static class TavilyQuotaDetector
{
    private static readonly string[] Keywords =
    [
        "credit",
        "credits",
        "quota",
        "exceeded",
        "exhausted"
    ];

    public static bool IsQuotaExhausted(int statusCode, string responseText)
    {
        if (statusCode is 432 or 433)
        {
            return true;
        }

        if (statusCode < 400)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(responseText))
        {
            return false;
        }

        return Keywords.Any(keyword => responseText.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}

