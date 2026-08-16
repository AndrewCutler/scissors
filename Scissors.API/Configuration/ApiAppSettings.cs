using Microsoft.Extensions.Configuration;

namespace Scissors.API.Configuration;

public sealed record ApiAppSettings
{
    public required string PostgresConnectionString { get; init; }
    public required GoogleOAuthSettings GoogleOAuth { get; init; }
    public required Jwt Jwt { get; init; }

    public static ApiAppSettings FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'Postgres' was not found.");
        }

        var googleSection = configuration.GetSection("OAuth:Google");

        var clientId = googleSection["ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException("OAuth:Google:ClientId cannot be null.");
        }

        var clientSecret = googleSection["ClientSecret"];
        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException("OAuth:Google:ClientSecret cannot be null.");
        }

        var redirectUri = googleSection["RedirectUri"];
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            throw new InvalidOperationException("OAuth:Google:RedirectUri cannot be null.");
        }

        var jwtSection = configuration.GetSection("Jwt");

        var issuer = jwtSection["Issuer"];
        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException("Jwt:Issuer cannot be null.");
        }

        var audience = jwtSection["Audience"];
        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("Jwt:Audience cannot be null.");
        }

        var secret = jwtSection["Secret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("Jwt:Secret cannot be null.");
        }

        return new ApiAppSettings
        {
            PostgresConnectionString = connectionString,
            GoogleOAuth = new GoogleOAuthSettings
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                RedirectUri = redirectUri,
            },
            Jwt = new Jwt
            {
                Issuer = issuer,
                Audience = audience,
                Secret = secret
            }
        };
    }
}

public sealed record Jwt
{
    public required string Issuer { get; set; } = string.Empty;
    public required string Audience { get; set; } = string.Empty;
    public required string Secret { get; set; } = string.Empty;
}

public sealed record GoogleOAuthSettings
{
    public required string ClientId { get; set; } = string.Empty;
    public required string ClientSecret { get; set; } = string.Empty;
    public required string RedirectUri { get; set; } = string.Empty;
}