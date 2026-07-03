using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DiscordBot.Infrastructure.Data;

/// <summary>
/// Used by EF Core CLI (dotnet ef) without bootstrapping the full API host.
/// Loads connection strings from DiscordBot.Api appsettings (same as local dev).
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found. " +
                "Set ConnectionStrings__DefaultConnection or configure appsettings.Development.json in DiscordBot.Api.");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }

    private static IConfiguration BuildConfiguration()
    {
        var apiProjectPath = ResolveApiProjectPath();
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        return new ConfigurationBuilder()
            .SetBasePath(apiProjectPath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static string ResolveApiProjectPath()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "src", "DiscordBot.Api");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            candidate = Path.Combine(current.FullName, "DiscordBot.Api");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate DiscordBot.Api for EF design-time configuration.");
    }
}
