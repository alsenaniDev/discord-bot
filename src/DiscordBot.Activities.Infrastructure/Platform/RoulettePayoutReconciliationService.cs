using DiscordBot.Activities.Application.Abstractions;
using DiscordBot.Activities.Application.Models;
using DiscordBot.Activities.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Activities.Infrastructure.Platform;

public sealed class RoulettePayoutReconciliationService(
    IServiceScopeFactory scopeFactory,
    ILogger<RoulettePayoutReconciliationService> logger) : BackgroundService
{
    private static readonly TimeSpan ProcessingLeaseTimeout = TimeSpan.FromMinutes(2);
    private readonly string _owner = $"{Environment.MachineName}:{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await RunOnceAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { logger.LogError(ex, "Roulette payout reconciliation cycle failed."); }
        }
    }

    public async Task RunOnceAsync(CancellationToken ct = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ActivitiesDbContext>();
        var platform = scope.ServiceProvider.GetRequiredService<IPlatformApiClient>();
        var now = DateTimeOffset.UtcNow;
        var staleCutoff = now.Subtract(ProcessingLeaseTimeout);

        var candidateIds = await db.RoulettePayouts.AsNoTracking()
            .Where(x => (x.Status == "PendingPayout"
                    || x.Status == "RetryableFailed"
                    || (x.Status == "Processing" && x.ProcessingStartedAtUtc <= staleCutoff))
                && x.RetryCount < 10
                && (x.NextAttemptAtUtc == null || x.NextAttemptAtUtc <= now))
            .OrderBy(x => x.NextAttemptAtUtc ?? x.LastAttemptAtUtc ?? x.CreatedAtUtc)
            .Select(x => x.Id)
            .Take(20)
            .ToListAsync(ct);

        var claimedIds = new List<Guid>();
        foreach (var id in candidateIds)
        {
            var claimed = await db.RoulettePayouts
                .Where(x => x.Id == id
                    && (x.Status == "PendingPayout"
                        || x.Status == "RetryableFailed"
                        || (x.Status == "Processing" && x.ProcessingStartedAtUtc <= staleCutoff))
                    && x.RetryCount < 10
                    && (x.NextAttemptAtUtc == null || x.NextAttemptAtUtc <= now))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, "Processing")
                    .SetProperty(x => x.ProcessingOwner, _owner)
                    .SetProperty(x => x.ProcessingStartedAtUtc, now)
                    .SetProperty(x => x.LastAttemptAtUtc, now)
                    .SetProperty(x => x.RetryCount, x => x.RetryCount + 1)
                    .SetProperty(x => x.LastError, (string?)null), ct);
            if (claimed == 1) claimedIds.Add(id);
        }

        var due = await db.RoulettePayouts
            .Include(x => x.RouletteRound).ThenInclude(x => x.RouletteGameSession).ThenInclude(x => x.GameSession)
            .Include(x => x.RouletteRound).ThenInclude(x => x.Payouts)
            .Where(x => claimedIds.Contains(x.Id) && x.ProcessingOwner == _owner)
            .ToListAsync(ct);

        foreach (var payout in due)
        {
            try
            {
                var game = payout.RouletteRound.RouletteGameSession.GameSession;
                var result = await platform.CreditWalletAsync(new WalletCreditRequest
                {
                    DiscordGuildId = game.DiscordGuildId,
                    DiscordUserId = payout.DiscordUserId,
                    GameKey = "roulette",
                    GameSessionId = game.Id,
                    RoundId = payout.RouletteRoundId,
                    PayoutId = payout.Id,
                    Amount = payout.Amount,
                    Currency = payout.Currency,
                    Reason = "roulette_payout",
                    IdempotencyKey = payout.IdempotencyKey
                }, ct);

                if (result.Succeeded)
                {
                    payout.Status = "Paid";
                    payout.PaidAtUtc = DateTimeOffset.UtcNow;
                    payout.NextAttemptAtUtc = null;
                    payout.ProcessingOwner = null;
                    payout.ProcessingStartedAtUtc = null;
                    payout.LastError = null;
                    if (payout.RouletteRound.Payouts.All(x => x.Id == payout.Id || x.Status == "Paid"))
                    {
                        payout.RouletteRound.Status = "Completed";
                        payout.RouletteRound.CompletedAtUtc ??= DateTimeOffset.UtcNow;
                    }
                    logger.LogInformation("Roulette payout {PayoutId} credited for gameSession {GameSessionId}, user {DiscordUserId}, amount {Amount}.", payout.Id, game.Id, payout.DiscordUserId, payout.Amount);
                }
                else
                {
                    payout.Status = result.Status == "Rejected" ? "Failed" : "RetryableFailed";
                    payout.LastError = result.ErrorMessage ?? "تعذر إضافة المكافأة.";
                    payout.NextAttemptAtUtc = payout.Status == "RetryableFailed" ? NextAttempt(payout.RetryCount) : null;
                    payout.ProcessingOwner = null;
                    payout.ProcessingStartedAtUtc = null;
                    logger.LogWarning("Roulette payout {PayoutId} failed with status {Status}: {Error}", payout.Id, payout.Status, payout.LastError);
                }
            }
            catch (Exception ex)
            {
                payout.Status = "RetryableFailed";
                payout.LastError = ex.Message[..Math.Min(ex.Message.Length, 500)];
                payout.NextAttemptAtUtc = NextAttempt(payout.RetryCount);
                payout.ProcessingOwner = null;
                payout.ProcessingStartedAtUtc = null;
                logger.LogWarning(ex, "Roulette payout {PayoutId} retry failed.", payout.Id);
            }
        }

        if (due.Count > 0) await db.SaveChangesAsync(ct);
    }

    private static DateTimeOffset NextAttempt(int retryCount) =>
        DateTimeOffset.UtcNow.AddSeconds(Math.Min(300, Math.Max(10, retryCount * 15)));
}
