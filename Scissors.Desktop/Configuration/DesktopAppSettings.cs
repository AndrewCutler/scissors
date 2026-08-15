using System;
using Microsoft.Extensions.Configuration;

namespace Scissors.Configuration;

public sealed record DesktopAppSettings
{
    public required string ApiUrl { get; init; }

    public required GoogleOAuthSettings GoogleOAuth { get; init; }

    public static DesktopAppSettings FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var apiUrl = configuration["ApiUrl"];
        if (string.IsNullOrWhiteSpace(apiUrl))
        {
            throw new InvalidOperationException("ApiUrl cannot be null.");
        }

        var googleSection = configuration.GetSection("OAuth:Google");

        var clientId = googleSection["ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException("OAuth:Google:ClientId cannot be null.");
        }

        var redirectUri = googleSection["RedirectUri"];
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            throw new InvalidOperationException("OAuth:Google:RedirectUri cannot be null.");
        }

        return new DesktopAppSettings
        {
            ApiUrl = apiUrl,
            GoogleOAuth = new GoogleOAuthSettings
            {
                ClientId = clientId,
                RedirectUri = redirectUri,
            },
        };
    }

    public static DesktopAppSettings CreateDesignTimeDefaults()
    {
        return new DesktopAppSettings
        {
            ApiUrl = "http://localhost:5098/api/v1",
            GoogleOAuth = new GoogleOAuthSettings
            {
                ClientId = "preview-client-id",
                RedirectUri = "http://localhost:3000/oauth/callback",
            },
        };
    }
}

public sealed record GoogleOAuthSettings
{
    public required string ClientId { get; init; }

    public required string RedirectUri { get; init; }
}
