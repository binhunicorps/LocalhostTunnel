namespace LocalhostTunnel.Application.Interfaces;

public interface ITavilyProxyHostFactory
{
    ITavilyProxyHost Create(string profileId);
}

