using DiscordBot.Domain.Constants;
using DiscordBot.Domain.Entities;
using DiscordBot.Domain.Extensions;
using DiscordBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Infrastructure.Data;

public class SubscriptionPlanSeeder : IHostedService
{
    private static readonly (string Key, string Name, string Description, string[] Modules)[] DefaultPlans =
    [
        (
            PlanKeys.Free,
            "Free",
            "Basic bot features for small servers.",
            [ModuleKeys.Welcome, ModuleKeys.Logs]
        ),
        (
            PlanKeys.Basic,
            "Basic",
            "Adds reaction roles on top of Free.",
            [ModuleKeys.Welcome, ModuleKeys.Logs, ModuleKeys.ReactionRoles]
        ),
        (
            PlanKeys.Pro,
            "Pro",
            "Tickets and moderation for growing communities.",
            [ModuleKeys.Welcome, ModuleKeys.Logs, ModuleKeys.ReactionRoles, ModuleKeys.Tickets, ModuleKeys.Moderation]
        ),
        (
            PlanKeys.Premium,
            "Premium",
            "All platform modules included.",
            [PlanKeys.AllModulesToken]
        )
    ];

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionPlanSeeder> _logger;

    public SubscriptionPlanSeeder(IServiceProvider serviceProvider, ILogger<SubscriptionPlanSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existingKeys = await dbContext.SubscriptionPlans
            .Select(p => p.Key)
            .ToListAsync(cancellationToken);

        var added = 0;
        foreach (var (key, name, description, modules) in DefaultPlans)
        {
            if (existingKeys.Contains(key))
            {
                continue;
            }

            dbContext.SubscriptionPlans.Add(new SubscriptionPlan
            {
                Key = key,
                Name = name,
                Description = description,
                AllowedModulesJson = PlanModulesExtensions.SerializeAllowedModules(modules),
                IsActive = true
            });
            added++;
        }

        if (added > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded {Count} subscription plan(s).", added);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
