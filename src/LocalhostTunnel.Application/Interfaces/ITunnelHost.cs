using LocalhostTunnel.Core.Runtime;

namespace LocalhostTunnel.Application.Interfaces;

public interface ITunnelHost
{
    TunnelSnapshot Current { get; }

    Task<TunnelStartResult> StartAsync(string tunnelToken, CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}

public sealed record TunnelStartResult(bool IsSuccess, string? ErrorMessage = null);
