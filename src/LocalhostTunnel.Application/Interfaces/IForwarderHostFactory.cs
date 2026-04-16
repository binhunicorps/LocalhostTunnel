namespace LocalhostTunnel.Application.Interfaces;

public interface IForwarderHostFactory
{
    IForwarderHost Create(string profileId);
}

