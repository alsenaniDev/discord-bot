using DiscordBot.Activities.Infrastructure.Data;
using DiscordBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace DiscordBot.Activities.IntegrationTests.PostgreSql;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithDatabase("postgres")
        .Build();

    public string ActivitiesConnectionString { get; private set; } = string.Empty;
    public string PlatformConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ActivitiesConnectionString = WithDatabase($"activities_tests_{Guid.NewGuid():N}");
        PlatformConnectionString = WithDatabase($"platform_tests_{Guid.NewGuid():N}");
        await using var activities = CreateActivitiesContext();
        await activities.Database.MigrateAsync();
        await using var platform = CreatePlatformContext();
        await platform.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public ActivitiesDbContext CreateActivitiesContext()
    {
        var options = new DbContextOptionsBuilder<ActivitiesDbContext>()
            .UseNpgsql(ActivitiesConnectionString)
            .Options;
        return new ActivitiesDbContext(options);
    }

    public AppDbContext CreatePlatformContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(PlatformConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    private string WithDatabase(string database)
    {
        var builder = new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = database };
        return builder.ConnectionString;
    }
}
