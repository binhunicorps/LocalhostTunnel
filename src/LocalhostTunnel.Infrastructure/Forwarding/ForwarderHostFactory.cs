using LocalhostTunnel.Application.Interfaces;

namespace LocalhostTunnel.Infrastructure.Forwarding;

public sealed class ForwarderHostFactory : IForwarderHostFactory
{
    public IForwarderHost Create(string profileId)
    {
        return new ForwarderHost();
    }
}

