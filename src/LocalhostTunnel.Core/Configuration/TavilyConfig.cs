using System.Text.Json.Serialization;

namespace LocalhostTunnel.Core.Configuration;

public sealed class TavilyConfig
{
    [JsonPropertyName("api_key_1")]
    public string ApiKey1 { get; init; } = string.Empty;

    [JsonPropertyName("api_key_2")]
    public string ApiKey2 { get; init; } = string.Empty;

    [JsonPropertyName("host")]
    public string Host { get; init; } = "127.0.0.1";

    [JsonPropertyName("port")]
    public int Port { get; init; } = 8766;

    [JsonPropertyName("base_url")]
    public string BaseUrl { get; init; } = "https://api.tavily.com";

    [JsonPropertyName("request_timeout_seconds")]
    public double RequestTimeoutSeconds { get; init; } = 60;
}

