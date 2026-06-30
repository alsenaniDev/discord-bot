using DiscordBot.Domain.Entities;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBot.Infrastructure.Data;

/// <summary>
/// Inserts a test guild in Development when Seed:Enabled is true.
/// </summary>
public class DevelopmentDataSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SeedOptions _seedOptions;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DevelopmentDataSeeder> _logger;

    public DevelopmentDataSeeder(
        IServiceProvider serviceProvider,
        IOptions<SeedOptions> seedOptions,
        IHostEnvironment environment,
        ILogger<DevelopmentDataSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _seedOptions = seedOptions.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment() || !_seedOptions.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_seedOptions.OwnerDiscordUserId))
        {
            _logger.LogWarning(
                "Seed:Enabled is true but Seed:OwnerDiscordUserId is empty. Skipping guild seed.");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var exists = await dbContext.Guilds.AnyAsync(
            g => g.DiscordGuildId == _seedOptions.DiscordGuildId,
            cancellationToken);

        if (exists)
        {
            return;
        }

        var guild = new Guild
        {
            DiscordGuildId = _seedOptions.DiscordGuildId,
            Name = _seedOptions.GuildName,
            OwnerDiscordUserId = _seedOptions.OwnerDiscordUserId,
            IsActive = true,
            Settings = new GuildSettings
            {
                WelcomeEnabled = true,
                WelcomeMessage = "Welcome {user} to {server}!",
                LogsEnabled = true
            }
        };

        dbContext.Guilds.Add(guild);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Seeded test guild '{GuildName}' for Discord user {OwnerDiscordUserId}.",
            guild.Name,
            guild.OwnerDiscordUserId);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
