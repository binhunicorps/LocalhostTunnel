using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Core.Configuration;
using LocalhostTunnel.Core.Runtime;

namespace LocalhostTunnel.Application.Services.Runtime;

public sealed class RuntimeManager
{
    private readonly object _sync = new();

    private readonly IProfilesConfigStore _profilesConfigStore;
    private readonly ITunnelHostFactory _tunnelHostFactory;
    private readonly IForwarderHostFactory _forwarderHostFactory;
    private readonly ITavilyProxyHostFactory _tavilyProxyHostFactory;
    private readonly ILogStore _logStore;

    private ProfilesConfig _profilesConfig = ProfilesConfig.CreateDefault();
    private readonly Dictionary<string, IRuntimeInstance> _instances = new(StringComparer.OrdinalIgnoreCase);

    public RuntimeManager(
        IProfilesConfigStore profilesConfigStore,
        ITunnelHostFactory tunnelHostFactory,
        IForwarderHostFactory forwarderHostFactory,
        ITavilyProxyHostFactory tavilyProxyHostFactory,
        ILogStore logStore)
    {
        _profilesConfigStore = profilesConfigStore;
        _tunnelHostFactory = tunnelHostFactory;
        _forwarderHostFactory = forwarderHostFactory;
        _tavilyProxyHostFactory = tavilyProxyHostFactory;
        _logStore = logStore;
    }

    public RuntimeSnapshot Current { get; private set; } = new();

    public event EventHandler<RuntimeSnapshot>? SnapshotUpdated;

    public IReadOnlyList<TunnelProfile> Profiles
    {
        get
        {
            lock (_sync)
            {
                return _profilesConfig.Profiles;
            }
        }
    }

    public string SelectedProfileId
    {
        get
        {
            lock (_sync)
            {
                return _profilesConfig.SelectedProfileId;
            }
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var loaded = await _profilesConfigStore.LoadAsync(cancellationToken);
        lock (_sync)
        {
            _profilesConfig = Normalize(loaded);
            foreach (var profile in _profilesConfig.Profiles)
            {
                if (_instances.TryGetValue(profile.Id, out var instance))
                {
                    instance.UpdateProfile(profile);
                }
            }
        }

        PublishSnapshot();
    }

    public async Task SaveAsync(ProfilesConfig config, CancellationToken cancellationToken)
    {
        var normalized = Normalize(config);
        await _profilesConfigStore.SaveAsync(normalized, cancellationToken);

        lock (_sync)
        {
            _profilesConfig = normalized;
            foreach (var profile in normalized.Profiles)
            {
                if (_instances.TryGetValue(profile.Id, out var instance))
                {
                    instance.UpdateProfile(profile);
                }
            }
        }

        PublishSnapshot();
    }

    public async Task SetSelectedProfileAsync(string profileId, CancellationToken cancellationToken)
    {
        ProfilesConfig next;
        lock (_sync)
        {
            next = new ProfilesConfig
            {
                SelectedProfileId = profileId,
                Profiles = _profilesConfig.Profiles
            };
        }

        await SaveAsync(next, cancellationToken);
    }

    public async Task StartProfileAsync(string profileId, CancellationToken cancellationToken)
    {
        IRuntimeInstance instance;
        lock (_sync)
        {
            var profile = _profilesConfig.Profiles.FirstOrDefault(p => string.Equals(p.Id, profileId, StringComparison.OrdinalIgnoreCase));
            if (profile is null)
            {
                throw new InvalidOperationException($"Profile '{profileId}' was not found.");
            }

            instance = GetOrCreateInstance(profile);
        }

        await instance.StartAsync(cancellationToken);
        PublishSnapshot();
    }

    public async Task StopProfileAsync(string profileId, CancellationToken cancellationToken)
    {
        IRuntimeInstance? instance;
        lock (_sync)
        {
            _instances.TryGetValue(profileId, out instance);
        }

        if (instance is null)
        {
            PublishSnapshot();
            return;
        }

        await instance.StopAsync(cancellationToken);
        PublishSnapshot();
    }

    public async Task RemoveProfileAsync(string profileId, CancellationToken cancellationToken)
    {
        await StopProfileAsync(profileId, cancellationToken);

        ProfilesConfig next;
        lock (_sync)
        {
            var remaining = _profilesConfig.Profiles
                .Where(p => !string.Equals(p.Id, profileId, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (remaining.Length == 0)
            {
                remaining = [CreateDefaultStandardProfile()];
            }

            var selected = remaining.Any(x => string.Equals(x.Id, _profilesConfig.SelectedProfileId, StringComparison.OrdinalIgnoreCase))
                ? _profilesConfig.SelectedProfileId
                : remaining[0].Id;

            next = new ProfilesConfig
            {
                SelectedProfileId = selected,
                Profiles = remaining
            };

            _instances.Remove(profileId);
        }

        await SaveAsync(next, cancellationToken);
    }

    public async Task StartEnabledProfilesAsync(CancellationToken cancellationToken)
    {
        var profileIds = Profiles.Where(x => x.Enabled).Select(x => x.Id).ToArray();
        foreach (var profileId in profileIds)
        {
            await StartProfileAsync(profileId, cancellationToken);
        }
    }

    public async Task StopAllAsync(CancellationToken cancellationToken)
    {
        string[] profileIds;
        lock (_sync)
        {
            profileIds = _instances.Keys.ToArray();
        }

        foreach (var profileId in profileIds)
        {
            await StopProfileAsync(profileId, cancellationToken);
        }

        PublishSnapshot();
    }

    public ProfileRuntimeSnapshot GetProfileSnapshot(string profileId)
    {
        TunnelProfile? profile;
        IRuntimeInstance? instance;

        lock (_sync)
        {
            profile = _profilesConfig.Profiles.FirstOrDefault(x => string.Equals(x.Id, profileId, StringComparison.OrdinalIgnoreCase));
            _instances.TryGetValue(profileId, out instance);
        }

        if (profile is null)
        {
            return new ProfileRuntimeSnapshot
            {
                ProfileId = profileId,
                ProfileName = "Unknown"
            };
        }

        return instance?.Snapshot ?? new ProfileRuntimeSnapshot
        {
            ProfileId = profile.Id,
            ProfileName = profile.Name
        };
    }

    public void PublishSnapshot()
    {
        ProfilesConfig config;
        List<ProfileRuntimeSnapshot> profileSnapshots = [];
        lock (_sync)
        {
            config = _profilesConfig;
            foreach (var profile in config.Profiles)
            {
                if (_instances.TryGetValue(profile.Id, out var instance))
                {
                    profileSnapshots.Add(instance.Snapshot);
                    continue;
                }

                profileSnapshots.Add(new ProfileRuntimeSnapshot
                {
                    ProfileId = profile.Id,
                    ProfileName = profile.Name
                });
            }
        }

        var selected = profileSnapshots.FirstOrDefault(x => string.Equals(x.ProfileId, config.SelectedProfileId, StringComparison.OrdinalIgnoreCase))
                       ?? profileSnapshots.FirstOrDefault();

        Current = new RuntimeSnapshot
        {
            CapturedAt = DateTimeOffset.UtcNow,
            SelectedProfileId = selected?.ProfileId ?? string.Empty,
            Tunnel = new TunnelSnapshot
            {
                State = selected?.TunnelState ?? ServiceState.Stopped,
                StartedAt = selected?.StartedAt,
                Uptime = selected?.Uptime ?? TimeSpan.Zero
            },
            Forwarder = new ForwarderSnapshot
            {
                State = selected?.ForwarderState ?? ServiceState.Stopped,
                StartedAt = selected?.StartedAt,
                Uptime = selected?.Uptime ?? TimeSpan.Zero
            },
            Profiles = profileSnapshots,
            Logs = _logStore.Entries.ToArray()
        };

        SnapshotUpdated?.Invoke(this, Current);
    }

    private IRuntimeInstance GetOrCreateInstance(TunnelProfile profile)
    {
        if (_instances.TryGetValue(profile.Id, out var existing))
        {
            existing.UpdateProfile(profile);
            return existing;
        }

        var tunnelHost = _tunnelHostFactory.Create(profile.Id);
        var forwarderHost = _forwarderHostFactory.Create(profile.Id);
        IRuntimeInstance instance = profile.Type switch
        {
            ProfileType.Tavily => new TavilyRuntimeInstance(
                profile,
                tunnelHost,
                forwarderHost,
                _tavilyProxyHostFactory.Create(profile.Id),
                _logStore),
            _ => new StandardRuntimeInstance(
                profile,
                tunnelHost,
                forwarderHost,
                _logStore)
        };

        _instances[profile.Id] = instance;
        return instance;
    }

    private static ProfilesConfig Normalize(ProfilesConfig config)
    {
        if (config.Profiles.Count == 0)
        {
            return ProfilesConfig.CreateDefault();
        }

        var selectedProfileId = config.SelectedProfileId;
        if (string.IsNullOrWhiteSpace(selectedProfileId) ||
            !config.Profiles.Any(x => string.Equals(x.Id, selectedProfileId, StringComparison.OrdinalIgnoreCase)))
        {
            selectedProfileId = config.Profiles[0].Id;
        }

        return new ProfilesConfig
        {
            SelectedProfileId = selectedProfileId,
            Profiles = config.Profiles
        };
    }

    private static TunnelProfile CreateDefaultStandardProfile()
    {
        return new TunnelProfile
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Default Tunnel",
            Type = ProfileType.Standard,
            Enabled = true
        };
    }
}

