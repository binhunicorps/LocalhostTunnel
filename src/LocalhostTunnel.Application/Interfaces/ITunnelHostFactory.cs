namespace LocalhostTunnel.Application.Interfaces;

public interface ITunnelHostFactory
{
    ITunnelHost Create(string profileId);
}

