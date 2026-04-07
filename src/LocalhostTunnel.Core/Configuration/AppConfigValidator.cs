namespace LocalhostTunnel.Core.Configuration;

public static class AppConfigValidator
{
    public static ValidationResult Validate(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(config.TunnelUrl))
        {
            errors[nameof(AppConfig.TunnelUrl)] = "Tunnel URL is required.";
        }
        else
        {
            var isAbsolute = Uri.TryCreate(config.TunnelUrl, UriKind.Absolute, out var tunnelUri);
            var hasSupportedScheme = tunnelUri is not null &&
                                     (tunnelUri.Scheme == Uri.UriSchemeHttp || tunnelUri.Scheme == Uri.UriSchemeHttps);

            if (!isAbsolute || !hasSupportedScheme)
            {
                errors[nameof(AppConfig.TunnelUrl)] = "Tunnel URL must start with http:// or https://.";
            }
        }

        if (string.IsNullOrWhiteSpace(config.TunnelToken))
        {
            errors[nameof(AppConfig.TunnelToken)] = "Tunnel token is required.";
        }

        if (config.TargetPort is <= 0 or > 65535)
        {
            errors[nameof(AppConfig.TargetPort)] = "Target port must be between 1 and 65535.";
        }

        return new ValidationResult(errors);
    }
}
