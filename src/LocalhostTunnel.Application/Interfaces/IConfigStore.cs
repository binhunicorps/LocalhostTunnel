using LocalhostTunnel.Core.Configuration;

namespace LocalhostTunnel.Application.Interfaces;

public interface IConfigStore
{
    Task<AppConfig> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AppConfig config, CancellationToken cancellationToken = default);
}
