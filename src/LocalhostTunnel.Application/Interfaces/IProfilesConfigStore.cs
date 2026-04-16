using LocalhostTunnel.Core.Configuration;

namespace LocalhostTunnel.Application.Interfaces;

public interface IProfilesConfigStore
{
    Task<ProfilesConfig> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(ProfilesConfig config, CancellationToken cancellationToken = default);
}

