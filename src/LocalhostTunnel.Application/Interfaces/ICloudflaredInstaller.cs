namespace LocalhostTunnel.Application.Interfaces;

public interface ICloudflaredInstaller
{
    Task EnsureInstalledAsync(CancellationToken cancellationToken);
}
