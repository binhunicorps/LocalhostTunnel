using System.Text.Json.Serialization;

namespace LocalhostTunnel.Core.Configuration;

public sealed class ProfilesConfig
{
    [JsonPropertyName("selected_profile_id")]
    public string SelectedProfileId { get; init; } = string.Empty;

    [JsonPropertyName("profiles")]
    public IReadOnlyList<TunnelProfile> Profiles { get; init; } = Array.Empty<TunnelProfile>();

    public static ProfilesConfig CreateDefault()
    {
        var standard = new TunnelProfile
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Default Tunnel",
            Type = ProfileType.Standard,
            Enabled = true
        };

        var tavily = new TunnelProfile
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Tavily API",
            Type = ProfileType.Tavily,
            Enabled = false,
            TargetHost = "127.0.0.1",
            TargetPort = 8766,
            Tavily = new TavilyConfig()
        };

        return new ProfilesConfig
        {
            SelectedProfileId = standard.Id,
            Profiles = new[] { standard, tavily }
        };
    }
}

