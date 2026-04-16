namespace LocalhostTunnel.Core.Configuration;

public static class TunnelProfileValidator
{
    public static ValidationResult Validate(TunnelProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(profile.Id))
        {
            errors[nameof(TunnelProfile.Id)] = "Profile id is required.";
        }

        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            errors[nameof(TunnelProfile.Name)] = "Profile name is required.";
        }

        var appValidation = AppConfigValidator.Validate(profile.ToAppConfig());
        foreach (var pair in appValidation.Errors)
        {
            errors[pair.Key] = pair.Value;
        }

        if (profile.Type == ProfileType.Tavily)
        {
            var tavily = profile.Tavily;
            if (tavily is null)
            {
                errors["Tavily"] = "Tavily configuration is required.";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(tavily.ApiKey1))
                {
                    errors["Tavily.ApiKey1"] = "Tavily API key 1 is required.";
                }

                if (string.IsNullOrWhiteSpace(tavily.ApiKey2))
                {
                    errors["Tavily.ApiKey2"] = "Tavily API key 2 is required.";
                }

                if (string.IsNullOrWhiteSpace(tavily.Host))
                {
                    errors["Tavily.Host"] = "Tavily host is required.";
                }

                if (tavily.Port is <= 0 or > 65535)
                {
                    errors["Tavily.Port"] = "Tavily port must be between 1 and 65535.";
                }

                if (tavily.RequestTimeoutSeconds <= 0)
                {
                    errors["Tavily.RequestTimeoutSeconds"] = "Tavily request timeout must be greater than zero.";
                }

                var baseUrlValid = Uri.TryCreate(tavily.BaseUrl, UriKind.Absolute, out var baseUri) &&
                                   baseUri is not null &&
                                   (baseUri.Scheme == Uri.UriSchemeHttp || baseUri.Scheme == Uri.UriSchemeHttps);
                if (!baseUrlValid)
                {
                    errors["Tavily.BaseUrl"] = "Tavily base url must start with http:// or https://.";
                }
            }
        }

        return new ValidationResult(errors);
    }
}

