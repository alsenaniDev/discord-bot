using ActivityGameSession = DiscordBot.Activities.Domain.Entities.GameSession;
using ActivityRouletteBet = DiscordBot.Activities.Domain.Entities.RouletteBet;
using ActivityRouletteGameSession = DiscordBot.Activities.Domain.Entities.RouletteGameSession;
using ActivityRoulettePayout = DiscordBot.Activities.Domain.Entities.RoulettePayout;
using ActivityRoulettePlayer = DiscordBot.Activities.Domain.Entities.RoulettePlayer;
using ActivityRouletteRound = DiscordBot.Activities.Domain.Entities.RouletteRound;
using ActivitySessionEntity = DiscordBot.Activities.Domain.Entities.ActivitySession;
using DiscordBot.Activities.Application.Abstractions;
using DiscordBot.Activities.Application.Models;
using DiscordBot.Activities.Infrastructure.Data;
using DiscordBot.Activities.Infrastructure.Platform;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DiscordBot.Activities.IntegrationTests.PostgreSql;

public sealed class PostgreSqlConcurrencyTests(PostgreSqlFixture pg) : IClassFixture<PostgreSqlFixture>
{
    [DockerFact]
    public async Task Two_simultaneous_player_inserts_for_same_user_create_one_player()
    {
        await using var setup = pg.CreateActivitiesContext();
        var seed = await SeedRouletteAsync(setup);
        var barrier = new Barrier(2);

        var results = await Task.WhenAll(
            InsertPlayer(seed.Roulette.Id, "200", barrier),
            InsertPlayer(seed.Roulette.Id, "200", barrier));

        results.Count(x => x).Should().Be(1);
        await using var verify = pg.CreateActivitiesContext();
        var count = await verify.RoulettePlayers.CountAsync(x => x.RouletteGameSessionId == seed.Roulette.Id && x.DiscordUserId == "200");
        count.Should().Be(1);
    }

    [DockerFact]
    public async Task Two_simultaneous_bets_with_same_idempotency_key_create_one_bet()
    {
        await using var setup = pg.CreateActivitiesContext();
        var seed = await SeedRouletteAsync(setup, withRound: true);
        var barrier = new Barrier(2);

        var results = await Task.WhenAll(
            InsertBet(seed.Round!.Id, "bet-same", barrier),
            InsertBet(seed.Round!.Id, "bet-same", barrier));

        results.Count(x => x).Should().Be(1);
        await using var verify = pg.CreateActivitiesContext();
        var count = await verify.RouletteBets.CountAsync(x => x.RouletteRoundId == seed.Round.Id && x.IdempotencyKey == "bet-same");
        count.Should().Be(1);
    }

    [DockerFact]
    public async Task Fresh_processing_payout_is_not_reclaimed()
    {
        await using var db = pg.CreateActivitiesContext();
        var seed = await SeedRouletteAsync(db, withRound: true);
        db.RoulettePayouts.Add(new ActivityRoulettePayout
        {
            RouletteRoundId = seed.Round!.Id,
            DiscordUserId = "100",
            Amount = 20,
            IdempotencyKey = "fresh-processing",
            Status = "Processing",
            ProcessingStartedAtUtc = DateTimeOffset.UtcNow,
            ProcessingOwner = "active-worker"
        });
        await db.SaveChangesAsync();

        var fake = new FakePlatformApiClient();
        await RunPayoutWorkerOnce(fake);

        var payout = await db.RoulettePayouts.AsNoTracking().SingleAsync(x => x.IdempotencyKey == "fresh-processing");
        payout.Status.Should().Be("Processing");
        fake.CreditCalls.Should().Be(0);
    }

    [DockerFact]
    public async Task Stale_processing_payout_is_reclaimed_and_paid()
    {
        await using var db = pg.CreateActivitiesContext();
        var seed = await SeedRouletteAsync(db, withRound: true);
        db.RoulettePayouts.Add(new ActivityRoulettePayout
        {
            RouletteRoundId = seed.Round!.Id,
            DiscordUserId = "100",
            Amount = 20,
            IdempotencyKey = "stale-processing",
            Status = "Processing",
            ProcessingStartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10),
            ProcessingOwner = "dead-worker"
        });
        await db.SaveChangesAsync();

        var fake = new FakePlatformApiClient();
        await RunPayoutWorkerOnce(fake);

        var payout = await db.RoulettePayouts.AsNoTracking().SingleAsync(x => x.IdempotencyKey == "stale-processing");
        payout.Status.Should().Be("Paid");
        payout.PaidAtUtc.Should().NotBeNull();
        fake.CreditCalls.Should().Be(1);
    }

    [DockerFact]
    public async Task Two_payout_workers_do_not_double_credit_same_stale_payout()
    {
        await using var db = pg.CreateActivitiesContext();
        var seed = await SeedRouletteAsync(db, withRound: true);
        db.RoulettePayouts.Add(new ActivityRoulettePayout
        {
            RouletteRoundId = seed.Round!.Id,
            DiscordUserId = "100",
            Amount = 20,
            IdempotencyKey = "stale-race",
            Status = "Processing",
            ProcessingStartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10),
            ProcessingOwner = "dead-worker"
        });
        await db.SaveChangesAsync();

        var fake = new FakePlatformApiClient();
        await Task.WhenAll(RunPayoutWorkerOnce(fake), RunPayoutWorkerOnce(fake));

        var payout = await db.RoulettePayouts.AsNoTracking().SingleAsync(x => x.IdempotencyKey == "stale-race");
        payout.Status.Should().Be("Paid");
        fake.CreditCalls.Should().Be(1);
    }

    [DockerFact]
    public async Task Permanent_payout_rejection_is_marked_failed_and_not_retried()
    {
        await using var db = pg.CreateActivitiesContext();
        var seed = await SeedRouletteAsync(db, withRound: true);
        db.RoulettePayouts.Add(new ActivityRoulettePayout { RouletteRoundId = seed.Round!.Id, DiscordUserId = "100", Amount = 20, IdempotencyKey = "permanent-failure", Status = "PendingPayout" });
        await db.SaveChangesAsync();

        var fake = new FakePlatformApiClient { PermanentReject = true };
        await RunPayoutWorkerOnce(fake);
        await RunPayoutWorkerOnce(fake);

        var payout = await db.RoulettePayouts.AsNoTracking().SingleAsync(x => x.IdempotencyKey == "permanent-failure");
        payout.Status.Should().Be("Failed");
        fake.CreditCalls.Should().Be(1);
    }

    private async Task<bool> InsertPlayer(Guid rouletteSessionId, string userId, Barrier barrier)
    {
        await using var db = pg.CreateActivitiesContext();
        barrier.SignalAndWait(TimeSpan.FromSeconds(10));
        db.RoulettePlayers.Add(new ActivityRoulettePlayer { RouletteGameSessionId = rouletteSessionId, DiscordUserId = userId, Username = userId, Position = 9 });
        try { await db.SaveChangesAsync(); return true; }
        catch (DbUpdateException) { return false; }
    }

    private async Task<bool> InsertBet(Guid roundId, string idempotencyKey, Barrier barrier)
    {
        await using var db = pg.CreateActivitiesContext();
        barrier.SignalAndWait(TimeSpan.FromSeconds(10));
        db.RouletteBets.Add(new ActivityRouletteBet { RouletteRoundId = roundId, DiscordUserId = "100", BetType = "number", BetValue = "7", Amount = 10, IdempotencyKey = idempotencyKey, Status = "Accepted" });
        try { await db.SaveChangesAsync(); return true; }
        catch (DbUpdateException) { return false; }
    }

    private async Task RunPayoutWorkerOnce(FakePlatformApiClient fake)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ActivitiesDbContext>(options => options.UseNpgsql(pg.ActivitiesConnectionString));
        services.AddSingleton<IPlatformApiClient>(fake);
        await using var provider = services.BuildServiceProvider();
        var worker = new RoulettePayoutReconciliationService(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<RoulettePayoutReconciliationService>.Instance);
        await worker.RunOnceAsync();
    }

    private static async Task<(ActivityRouletteGameSession Roulette, ActivityRouletteRound? Round)> SeedRouletteAsync(ActivitiesDbContext db, bool withRound = false)
    {
        var unique = Guid.NewGuid().ToString("N");
        var activity = new ActivitySessionEntity
        {
            DiscordUserId = "100",
            Username = "tester",
            DiscordGuildId = "123456789012345678",
            DiscordChannelId = "123456789012345679",
            DiscordActivityInstanceId = "instance",
            GameKey = "roulette",
            GameVersion = "1.0.0",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1)
        };
        var game = new ActivityGameSession { ActivitySession = activity, GameKey = "roulette", GameVersion = "1.0.0", DiscordGuildId = activity.DiscordGuildId, DiscordChannelId = activity.DiscordChannelId, Status = "Waiting" };
        var roulette = new ActivityRouletteGameSession
        {
            GameSession = game,
            HostUserDiscordId = "100",
            HostUsername = "tester",
            Status = "Waiting",
            WinnerCoins = 100,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5)
        };
        roulette.Players.Add(new ActivityRoulettePlayer { DiscordUserId = "100", Username = "tester", IsHost = true, Position = 1 });
        ActivityRouletteRound? round = null;
        if (withRound)
        {
            round = new ActivityRouletteRound { RouletteGameSession = roulette, RoundNumber = 1, SpinnerUserDiscordId = "100", IdempotencyKey = $"round-{unique}" };
            roulette.Rounds.Add(round);
        }
        db.RouletteGameSessions.Add(roulette);
        await db.SaveChangesAsync();
        return (roulette, round);
    }

    private sealed class FakePlatformApiClient : IPlatformApiClient
    {
        private readonly HashSet<Guid> _credited = [];
        public int CreditCalls { get; private set; }
        public bool PermanentReject { get; set; }

        public Task<GameAccessResult> ValidateGameAccessAsync(ValidateGameAccessRequest request, CancellationToken cancellationToken = default) => Task.FromResult(new GameAccessResult { Allowed = true, GameKey = request.GameKey });
        public Task<WalletReservationResult> ReserveWalletAsync(ReserveWalletRequest request, CancellationToken cancellationToken = default) => Task.FromResult(new WalletReservationResult { Succeeded = true, ReservationId = Guid.NewGuid().ToString("N"), Status = "Pending" });
        public Task CommitWalletReservationAsync(string reservationId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ReleaseWalletReservationAsync(string reservationId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<WalletCreditResult> CreditWalletAsync(WalletCreditRequest request, CancellationToken cancellationToken = default)
        {
            if (PermanentReject)
            {
                CreditCalls++;
                return Task.FromResult(new WalletCreditResult { Succeeded = false, Status = "Rejected", ErrorMessage = "رفض دائم" });
            }

            lock (_credited)
            {
                if (_credited.Add(request.PayoutId)) CreditCalls++;
            }

            return Task.FromResult(new WalletCreditResult { Succeeded = true, Status = "Credited", Amount = request.Amount });
        }
    }
}
