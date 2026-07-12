using DiscordBot.Activities.Domain.Roulette;
using DiscordBot.Activities.Infrastructure.Data;
using DiscordBot.Activities.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBot.Activities.Infrastructure.Platform;

public sealed class RouletteLifecycleCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<RouletteRuntimeOptions> options,
    ILogger<RouletteLifecycleCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Clamp(options.Value.CleanupIntervalSeconds, 15, 3600));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Activities Roulette lifecycle cleanup failed. ExceptionType={ExceptionType}, ExceptionMessage={ExceptionMessage}.", ex.GetType().Name, ex.Message);
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    internal async Task ProcessOnceAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ActivitiesDbContext>();
        var now = DateTimeOffset.UtcNow;
        var runtime = options.Value;
        var abandonmentCutoff = now.AddMinutes(-Math.Max(1, runtime.InProgressAbandonmentMinutes));

        var expiredWaiting = await db.RouletteGameSessions
            .Where(x => x.Status == RouletteRuntimeStates.WaitingForPlayers && x.ExpiresAtUtc <= now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, RouletteRuntimeStates.Expired)
                .SetProperty(x => x.CompletedAtUtc, now)
                .SetProperty(x => x.CurrentTurnUserDiscordId, (string?)null)
                .SetProperty(x => x.UpdatedAtUtc, now), ct);

        var expiredGameSessions = await db.GameSessions
            .Where(x => x.Status == RouletteRuntimeStates.WaitingForPlayers && x.Roulette != null && x.Roulette.Status == RouletteRuntimeStates.Expired)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, RouletteRuntimeStates.Expired)
                .SetProperty(x => x.CompletedAtUtc, now)
                .SetProperty(x => x.UpdatedAtUtc, now), ct);

        var abandoned = await db.RouletteGameSessions
            .Where(x => x.Status == RouletteRuntimeStates.BettingOpen && x.UpdatedAtUtc <= abandonmentCutoff)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, RouletteRuntimeStates.Abandoned)
                .SetProperty(x => x.CompletedAtUtc, now)
                .SetProperty(x => x.CurrentTurnUserDiscordId, (string?)null)
                .SetProperty(x => x.PendingTargetUserDiscordId, (string?)null)
                .SetProperty(x => x.PendingActionStatus, "None")
                .SetProperty(x => x.PendingActionExpiresAtUtc, (DateTimeOffset?)null)
                .SetProperty(x => x.UpdatedAtUtc, now), ct);

        var abandonedGameSessions = await db.GameSessions
            .Where(x => x.Status == RouletteRuntimeStates.BettingOpen && x.Roulette != null && x.Roulette.Status == RouletteRuntimeStates.Abandoned)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, RouletteRuntimeStates.Abandoned)
                .SetProperty(x => x.CompletedAtUtc, now)
                .SetProperty(x => x.UpdatedAtUtc, now), ct);

        if (expiredWaiting + expiredGameSessions + abandoned + abandonedGameSessions > 0)
        {
            logger.LogInformation(
                "Activities Roulette cleanup completed. ExpiredWaiting={ExpiredWaiting}, ExpiredGameSessions={ExpiredGameSessions}, Abandoned={Abandoned}, AbandonedGameSessions={AbandonedGameSessions}, AbandonmentCutoff={AbandonmentCutoff}.",
                expiredWaiting,
                expiredGameSessions,
                abandoned,
                abandonedGameSessions,
                abandonmentCutoff);
        }
    }
}
