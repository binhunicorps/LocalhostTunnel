using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Core.Logging;
using LocalhostTunnel.Core.Runtime;
using LocalhostTunnel.Infrastructure.Storage;
using System.Diagnostics;

namespace LocalhostTunnel.Infrastructure.Tunnel;

public sealed class CloudflaredProcessHost : ITunnelHost
{
    private readonly object _sync = new();

    private readonly ICloudflaredInstaller _installer;
    private readonly ILogStore _logStore;
    private readonly CloudflaredOutputParser _outputParser;
    private readonly string _cloudflaredExePath;
    private readonly string _profileId;

    private Process? _process;
    private TunnelSnapshot _current = new();
    private DateTimeOffset? _startedAt;
    private bool _isStopping;

    public CloudflaredProcessHost(
        ICloudflaredInstaller installer,
        ILogStore logStore,
        AppDataPaths paths)
        : this(installer, logStore, paths, string.Empty, new CloudflaredOutputParser())
    {
    }

    public CloudflaredProcessHost(
        ICloudflaredInstaller installer,
        ILogStore logStore,
        AppDataPaths paths,
        string profileId)
        : this(installer, logStore, paths, profileId, new CloudflaredOutputParser())
    {
    }

    internal CloudflaredProcessHost(
        ICloudflaredInstaller installer,
        ILogStore logStore,
        AppDataPaths paths,
        string profileId,
        CloudflaredOutputParser outputParser)
    {
        ArgumentNullException.ThrowIfNull(installer);
        ArgumentNullException.ThrowIfNull(logStore);
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(outputParser);

        _installer = installer;
        _logStore = logStore;
        _outputParser = outputParser;
        _cloudflaredExePath = Path.Combine(paths.CloudflaredDirectory, "cloudflared.exe");
        _profileId = profileId;
    }

    public TunnelSnapshot Current
    {
        get
        {
            lock (_sync)
            {
                if (_current.State == ServiceState.Running && _startedAt.HasValue)
                {
                    return TunnelSnapshot.CreateRunning(_startedAt.Value);
                }

                return _current;
            }
        }
    }

    public async Task<TunnelStartResult> StartAsync(string tunnelToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tunnelToken))
        {
            return new TunnelStartResult(false, "Tunnel token is required.");
        }

        lock (_sync)
        {
            if (_process is not null && !_process.HasExited)
            {
                return new TunnelStartResult(true);
            }

            _isStopping = false;
            _current = new TunnelSnapshot
            {
                State = ServiceState.Starting
            };
        }

        try
        {
            await _installer.EnsureInstalledAsync(cancellationToken);

            if (!File.Exists(_cloudflaredExePath))
            {
                lock (_sync)
                {
                    _current = new TunnelSnapshot
                    {
                        State = ServiceState.Faulted
                    };
                }

                return new TunnelStartResult(false, "cloudflared executable was not found after installation.");
            }

            var process = CreateProcess(tunnelToken);
            process.OutputDataReceived += (_, eventArgs) => HandleOutput(eventArgs.Data);
            process.ErrorDataReceived += (_, eventArgs) => HandleOutput(eventArgs.Data);
            process.Exited += (_, _) => HandleExit(process);

            if (!process.Start())
            {
                process.Dispose();
                lock (_sync)
                {
                    _current = new TunnelSnapshot
                    {
                        State = ServiceState.Faulted
                    };
                }

                return new TunnelStartResult(false, "Failed to start cloudflared process.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            lock (_sync)
            {
                _process = process;
                _startedAt = DateTimeOffset.UtcNow;
                _current = new TunnelSnapshot
                {
                    State = ServiceState.Starting,
                    StartedAt = _startedAt
                };
            }

            return new TunnelStartResult(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logStore.Append(LogEntry.Error("cloudflared", $"failed to start cloudflared: {ex.Message}", _profileId));
            lock (_sync)
            {
                _current = new TunnelSnapshot
                {
                    State = ServiceState.Faulted
                };
                _startedAt = null;
                _process = null;
            }

            return new TunnelStartResult(false, ex.Message);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Process? processToStop;

        lock (_sync)
        {
            processToStop = _process;
            _isStopping = true;
            _current = new TunnelSnapshot
            {
                State = ServiceState.Stopping,
                StartedAt = _startedAt
            };
            _process = null;
        }

        if (processToStop is not null)
        {
            try
            {
                if (!processToStop.HasExited)
                {
                    processToStop.Kill(entireProcessTree: true);
                    await processToStop.WaitForExitAsync(cancellationToken);
                }
            }
            catch (InvalidOperationException)
            {
            }
            finally
            {
                processToStop.Dispose();
            }
        }

        lock (_sync)
        {
            _startedAt = null;
            _isStopping = false;
            _current = new TunnelSnapshot
            {
                State = ServiceState.Stopped
            };
        }
    }

    private Process CreateProcess(string tunnelToken)
    {
        return new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _cloudflaredExePath,
                Arguments = $"tunnel run --token {tunnelToken}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            },
            EnableRaisingEvents = true
        };
    }

    private void HandleOutput(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        _logStore.Append(LogEntry.Info("cloudflared", line, _profileId));
        var parsed = _outputParser.Parse(line);

        lock (_sync)
        {
            if (parsed.State == ServiceState.Running && !_startedAt.HasValue)
            {
                _startedAt = DateTimeOffset.UtcNow;
            }

            _current = new TunnelSnapshot
            {
                State = parsed.State,
                StartedAt = _startedAt
            };
        }
    }

    private void HandleExit(Process process)
    {
        lock (_sync)
        {
            if (_process == process)
            {
                _process = null;
            }

            if (_isStopping)
            {
                return;
            }

            _current = new TunnelSnapshot
            {
                State = ServiceState.Faulted
            };
            _startedAt = null;
        }
    }
}
