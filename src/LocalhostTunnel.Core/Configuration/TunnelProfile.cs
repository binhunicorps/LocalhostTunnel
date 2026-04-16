using System.Text.Json.Serialization;

namespace LocalhostTunnel.Core.Configuration;

public sealed record TunnelProfile
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("name")]
    public string Name { get; init; } = "New Profile";

    [JsonPropertyName("type")]
    public ProfileType Type { get; init; } = ProfileType.Standard;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;

    [JsonPropertyName("tunnel_url")]
    public string TunnelUrl { get; init; } = string.Empty;

    [JsonPropertyName("tunnel_token")]
    public string TunnelToken { get; init; } = string.Empty;

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
    public string WebhookSecret { get; init; } = string.Empty;

    [JsonPropertyName("max_body_size")]
    public int MaxBodySize { get; init; } = 10 * 1024 * 1024;

    [JsonPropertyName("upstream_timeout")]
    public int UpstreamTimeout { get; init; } = 30000;

    [JsonPropertyName("log_level")]
    public string LogLevel { get; init; } = "info";

    [JsonPropertyName("tavily")]
    public TavilyConfig? Tavily { get; init; }

    public AppConfig ToAppConfig()
    {
        return new AppConfig
        {
            TunnelUrl = TunnelUrl,
            TunnelToken = TunnelToken,
            TargetPort = TargetPort,
            Port = Port,
            Host = Host,
            TargetHost = TargetHost,
            TargetProtocol = TargetProtocol,
            WebhookSecret = WebhookSecret,
            MaxBodySize = MaxBodySize,
            UpstreamTimeout = UpstreamTimeout,
            LogLevel = LogLevel
        };
    }

    public TunnelProfile WithAppConfig(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return this with
        {
            TunnelUrl = config.TunnelUrl,
            TunnelToken = config.TunnelToken,
            TargetPort = config.TargetPort,
            Port = config.Port,
            Host = config.Host,
            TargetHost = config.TargetHost,
            TargetProtocol = config.TargetProtocol,
            WebhookSecret = config.WebhookSecret,
            MaxBodySize = config.MaxBodySize,
            UpstreamTimeout = config.UpstreamTimeout,
            LogLevel = config.LogLevel
        };
    }
}
