using DiscordBot.Activities.Application.Abstractions;
using DiscordBot.Activities.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Activities.Infrastructure.Platform;

public sealed class RouletteWalletReconciliationService(
    IServiceScopeFactory scopeFactory,
    ILogger<RouletteWalletReconciliationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await ReconcileAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { logger.LogError(ex, "Roulette wallet reconciliation cycle failed."); }
        }
    }

    private async Task ReconcileAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ActivitiesDbContext>();
        var platform = scope.ServiceProvider.GetRequiredService<IPlatformApiClient>();
        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-30);

        var pending = await db.RouletteBets
            .Where(x => x.Status == "PendingCommit" && x.WalletReservationId != null && x.UpdatedAtUtc <= cutoff)
            .OrderBy(x => x.UpdatedAtUtc)
            .Take(25)
            .ToListAsync(ct);

        foreach (var bet in pending)
        {
            try
            {
                await platform.CommitWalletReservationAsync(bet.WalletReservationId!, ct);
                bet.Status = "Accepted";
                bet.UpdatedAtUtc = DateTimeOffset.UtcNow;
                logger.LogInformation("Roulette wallet reconciliation committed reservation {ReservationId} for bet {BetId}.", bet.WalletReservationId, bet.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Roulette wallet reconciliation could not commit reservation {ReservationId} for bet {BetId}.", bet.WalletReservationId, bet.Id);
            }
        }

        if (pending.Count > 0) await db.SaveChangesAsync(ct);
    }
}
