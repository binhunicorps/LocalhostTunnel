using LocalhostTunnel.Core.Configuration;

namespace LocalhostTunnel.Application.Interfaces;

public interface ISessionStore
{
    Task<SessionState> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(SessionState sessionState, CancellationToken cancellationToken = default);
}
