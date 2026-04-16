using LocalhostTunnel.Application.Interfaces;

namespace LocalhostTunnel.Infrastructure.Tavily;

public sealed class TavilyProxyHostFactory : ITavilyProxyHostFactory
{
    private readonly ILogStore _logStore;

    public TavilyProxyHostFactory(ILogStore logStore)
    {
        _logStore = logStore;
    }

    public ITavilyProxyHost Create(string profileId)
    {
        return new TavilyProxyHost(_logStore, profileId);
    }
}

