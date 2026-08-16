using Microsoft.Extensions.Configuration;

namespace Scissors.API.Configuration;

public sealed record ApiAppSettings
{
    public required string PostgresConnectionString { get; init; }
    public required GoogleOAuthSettings GoogleOAuth { get; init; }

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

        return new ApiAppSettings
        {
            PostgresConnectionString = connectionString,
            GoogleOAuth = new GoogleOAuthSettings
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                RedirectUri = redirectUri,
            }
        };
    }
}


public sealed record GoogleOAuthSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}