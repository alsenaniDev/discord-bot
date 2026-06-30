using DiscordBot.Domain.Entities;
using DiscordBot.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBot.Infrastructure.Data;

/// <summary>
/// Seeds the configured platform admin on startup.
/// </summary>
public class PlatformAdminSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AdminOptions _adminOptions;
    private readonly ILogger<PlatformAdminSeeder> _logger;

    public PlatformAdminSeeder(
        IServiceProvider serviceProvider,
        IOptions<AdminOptions> adminOptions,
        ILogger<PlatformAdminSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _adminOptions = adminOptions.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_adminOptions.DiscordUserId))
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var discordUserId = _adminOptions.DiscordUserId.Trim();
        var exists = await dbContext.PlatformAdmins
            .AnyAsync(a => a.DiscordUserId == discordUserId, cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.PlatformAdmins.Add(new PlatformAdmin
        {
            DiscordUserId = discordUserId
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded platform admin for Discord user {DiscordUserId}.", discordUserId);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
