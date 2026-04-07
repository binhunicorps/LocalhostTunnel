using FluentAssertions;
using LocalhostTunnel.Core.Configuration;

namespace LocalhostTunnel.Core.Tests.Configuration;

public class AppConfigValidatorTests
{
    [Fact]
    public void Validate_Rejects_Empty_TunnelToken_And_Invalid_TargetPort()
    {
        var config = new AppConfig
        {
            TunnelUrl = "https://example.trycloudflare.com/",
            TunnelToken = "",
            TargetPort = 0
        };

        var result = AppConfigValidator.Validate(config);

        result.Errors.Should().ContainKey(nameof(AppConfig.TunnelToken));
        result.Errors.Should().ContainKey(nameof(AppConfig.TargetPort));
    }

    [Fact]
    public void Validate_Rejects_Empty_TunnelUrl()
    {
        var config = new AppConfig
        {
            TunnelUrl = "",
            TunnelToken = "token",
            TargetPort = 8765
        };

        var result = AppConfigValidator.Validate(config);

        result.Errors.Should().ContainKey(nameof(AppConfig.TunnelUrl));
    }

    [Fact]
    public void Validate_Rejects_TunnelUrl_Without_Http_Scheme()
    {
        var config = new AppConfig
        {
            TunnelUrl = "proxypal.wigdealer.net/api/health",
            TunnelToken = "token",
            TargetPort = 8765
        };

        var result = AppConfigValidator.Validate(config);

        result.Errors.Should().ContainKey(nameof(AppConfig.TunnelUrl));
    }

    [Fact]
    public void Validate_Accepts_TunnelUrl_With_Https_Scheme()
    {
        var config = new AppConfig
        {
            TunnelUrl = "https://proxypal.wigdealer.net/api/health",
            TunnelToken = "token",
            TargetPort = 8765
        };

        var result = AppConfigValidator.Validate(config);

        result.Errors.Should().NotContainKey(nameof(AppConfig.TunnelUrl));
    }
}
