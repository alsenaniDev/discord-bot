using DiscordBot.Domain.Constants;
using DiscordBot.Domain.Entities;
using DiscordBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Infrastructure.Data;

/// <summary>
/// Seeds the global module catalog on startup.
/// </summary>
public class ModuleSeeder : IHostedService
{
    private static readonly (string Key, string Name, string Description)[] DefaultModules =
    [
        (ModuleKeys.Welcome, "Welcome", "Send welcome messages when members join."),
        (ModuleKeys.Tickets, "Tickets", "Support ticket commands and tracking."),
        (ModuleKeys.Moderation, "Moderation", "Warn, kick, clear, and moderation cases."),
        (ModuleKeys.Logs, "Logs", "Send server event logs to a channel."),
        (ModuleKeys.AutoRole, "Auto Role", "Automatically assign a role when members join."),
        (ModuleKeys.ReactionRoles, "Reaction Roles", "Button-based role assignment panels.")
    ];

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ModuleSeeder> _logger;

    public ModuleSeeder(IServiceProvider serviceProvider, ILogger<ModuleSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existingKeys = await dbContext.Modules
            .Select(m => m.Key)
            .ToListAsync(cancellationToken);

        var added = 0;
        foreach (var (key, name, description) in DefaultModules)
        {
            if (existingKeys.Contains(key))
            {
                continue;
            }

            dbContext.Modules.Add(new Module
            {
                Key = key,
                Name = name,
                Description = description
            });
            added++;
        }

        if (added > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded {Count} module(s) into the catalog.", added);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
