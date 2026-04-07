using System.Text.Json.Serialization;

namespace LocalhostTunnel.Core.Configuration;

public sealed class AppConfig
{
    [JsonPropertyName("tunnel_url")]
    public string TunnelUrl { get; init; } = "";

    [JsonPropertyName("tunnel_token")]
    public string TunnelToken { get; init; } = "";

    [JsonPropertyName("target_port")]
    public int TargetPort { get; init; } = 8765;

    [JsonPropertyName("port")]
    public int Port { get; init; } = 8788;

    [JsonPropertyName("host")]
    public string Host { get; init; } = "127.0.0.1";

    [JsonPropertyName("target_host")]
    public string TargetHost { get; init; } = "127.0.0.1";

    [JsonPropertyName("target_protocol")]
    public string TargetProtocol { get; init; } = "http";

    [JsonPropertyName("webhook_secret")]
    public string WebhookSecret { get; init; } = "";

    [JsonPropertyName("max_body_size")]
    public int MaxBodySize { get; init; } = 10 * 1024 * 1024;

    [JsonPropertyName("upstream_timeout")]
    public int UpstreamTimeout { get; init; } = 30000;

    [JsonPropertyName("log_level")]
    public string LogLevel { get; init; } = "info";
}
