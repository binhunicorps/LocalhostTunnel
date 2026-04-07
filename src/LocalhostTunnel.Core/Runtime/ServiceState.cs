namespace LocalhostTunnel.Core.Runtime;

public enum ServiceState
{
    Stopped,
    Starting,
    Running,
    Stopping,
    Degraded,
    Faulted
}
