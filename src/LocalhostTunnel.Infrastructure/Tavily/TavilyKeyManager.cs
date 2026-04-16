namespace LocalhostTunnel.Infrastructure.Tavily;

public sealed class TavilyKeyManager
{
    private readonly object _sync = new();
    private string _activeKey;
    private string _fallbackKey;

    public TavilyKeyManager(string primaryKey, string fallbackKey)
    {
        _activeKey = primaryKey ?? string.Empty;
        _fallbackKey = fallbackKey ?? string.Empty;
    }

    public (string ActiveKey, string FallbackKey) GetKeyOrder()
    {
        lock (_sync)
        {
            return (_activeKey, _fallbackKey);
        }
    }

    public void Promote(string key)
    {
        lock (_sync)
        {
            if (!string.Equals(key, _fallbackKey, StringComparison.Ordinal))
            {
                return;
            }

            (_activeKey, _fallbackKey) = (_fallbackKey, _activeKey);
        }
    }
}

