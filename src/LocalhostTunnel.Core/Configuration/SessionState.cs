using System.Text.Json.Serialization;

namespace LocalhostTunnel.Core.Configuration;

public sealed class SessionState
{
    [JsonPropertyName("last_active_screen")]
    public string LastActiveScreen { get; init; } = "";

    [JsonPropertyName("last_active_profile_id")]
    public string LastActiveProfileId { get; init; } = "";

    [JsonPropertyName("window_left")]
    public int? WindowLeft { get; init; }

    [JsonPropertyName("window_top")]
    public int? WindowTop { get; init; }

    [JsonPropertyName("window_width")]
    public int WindowWidth { get; init; } = 1280;

    [JsonPropertyName("window_height")]
    public int WindowHeight { get; init; } = 720;

    [JsonPropertyName("logs_auto_scroll_enabled")]
    public bool LogsAutoScrollEnabled { get; init; } = true;
}
