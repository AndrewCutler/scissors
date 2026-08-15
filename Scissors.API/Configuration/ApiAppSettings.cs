using Microsoft.Extensions.Configuration;

namespace Scissors.API.Configuration;

public sealed record ApiAppSettings
{
    public required string PostgresConnectionString { get; init; }

    public static ApiAppSettings FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'Postgres' was not found.");
        }

        return new ApiAppSettings
        {
            PostgresConnectionString = connectionString,
        };
    }
}
