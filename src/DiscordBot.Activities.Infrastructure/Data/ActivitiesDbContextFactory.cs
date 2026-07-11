using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DiscordBot.Activities.Infrastructure.Data;

public class ActivitiesDbContextFactory : IDesignTimeDbContextFactory<ActivitiesDbContext>
{
    public ActivitiesDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();

        var connectionString = configuration.GetConnectionString("ActivitiesDatabase")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'ActivitiesDatabase' not found. " +
                "Set ConnectionStrings__ActivitiesDatabase or configure appsettings.Development.json in DiscordBot.Activities.Api.");

        var options = new DbContextOptionsBuilder<ActivitiesDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ActivitiesDbContext(options);
    }

    private static IConfiguration BuildConfiguration()
    {
        var apiProjectPath = ResolveActivitiesApiProjectPath();
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        return new ConfigurationBuilder()
            .SetBasePath(apiProjectPath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static string ResolveActivitiesApiProjectPath()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "src", "DiscordBot.Activities.Api");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            candidate = Path.Combine(current.FullName, "DiscordBot.Activities.Api");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate DiscordBot.Activities.Api for EF design-time configuration.");
    }
}
