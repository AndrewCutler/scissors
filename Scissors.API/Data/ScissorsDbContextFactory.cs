using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Scissors.API.Configuration;

namespace Scissors.API.Data;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<ScissorsDbContext>
{
    public ScissorsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var appSettings = ApiAppSettings.FromConfiguration(configuration);

        var optionsBuilder = new DbContextOptionsBuilder<ScissorsDbContext>();
        optionsBuilder.UseNpgsql(appSettings.PostgresConnectionString);

        return new ScissorsDbContext(optionsBuilder.Options);
    }
}
