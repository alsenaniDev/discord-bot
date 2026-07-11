using ActivityGameSession = DiscordBot.Activities.Domain.Entities.GameSession;
using ActivityRouletteBet = DiscordBot.Activities.Domain.Entities.RouletteBet;
using ActivityRouletteGameSession = DiscordBot.Activities.Domain.Entities.RouletteGameSession;
using ActivityRoulettePayout = DiscordBot.Activities.Domain.Entities.RoulettePayout;
using ActivityRoulettePlayer = DiscordBot.Activities.Domain.Entities.RoulettePlayer;
using ActivityRouletteRound = DiscordBot.Activities.Domain.Entities.RouletteRound;
using ActivitySessionEntity = DiscordBot.Activities.Domain.Entities.ActivitySession;
using DiscordBot.Activities.Application.Realtime;
using DiscordBot.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DiscordBot.Activities.IntegrationTests.PostgreSql;

public sealed class PostgreSqlConstraintTests(PostgreSqlFixture pg) : IClassFixture<PostgreSqlFixture>
{
    [DockerFact]
    public void SignalR_group_helper_matches_expected_publisher_and_hub_key()
    {
        var gameSessionId = Guid.NewGuid();

        GameSessionGroupNames.Roulette(gameSessionId).Should().Be($"game-session:{gameSessionId}");
    }

    [DockerFact]
    public async Task Duplicate_roulette_player_membership_is_rejected()
    {
        await using var db = pg.CreateActivitiesContext();
        var session = await SeedRouletteAsync(db);

        db.RoulettePlayers.Add(new ActivityRoulettePlayer { RouletteGameSessionId = session.Roulette.Id, DiscordUserId = "100", Username = "same", Position = 2 });
        db.RoulettePlayers.Add(new ActivityRoulettePlayer { RouletteGameSessionId = session.Roulette.Id, DiscordUserId = "100", Username = "same", Position = 3 });

        await AssertDbUpdateFails(() => db.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Duplicate_bet_idempotency_key_is_rejected()
    {
        await using var db = pg.CreateActivitiesContext();
        var session = await SeedRouletteAsync(db, withRound: true);
        var roundId = session.Round!.Id;

        db.RouletteBets.Add(new ActivityRouletteBet { RouletteRoundId = roundId, DiscordUserId = "100", BetType = "number", BetValue = "7", Amount = 10, IdempotencyKey = "same-key", Status = "Accepted" });
        db.RouletteBets.Add(new ActivityRouletteBet { RouletteRoundId = roundId, DiscordUserId = "100", BetType = "number", BetValue = "7", Amount = 10, IdempotencyKey = "same-key", Status = "Accepted" });

        await AssertDbUpdateFails(() => db.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Duplicate_round_number_within_session_is_rejected()
    {
        await using var db = pg.CreateActivitiesContext();
        var session = await SeedRouletteAsync(db);

        db.RouletteRounds.Add(new ActivityRouletteRound { RouletteGameSessionId = session.Roulette.Id, RoundNumber = 1, SpinnerUserDiscordId = "100", IdempotencyKey = "round-a" });
        db.RouletteRounds.Add(new ActivityRouletteRound { RouletteGameSessionId = session.Roulette.Id, RoundNumber = 1, SpinnerUserDiscordId = "100", IdempotencyKey = "round-b" });

        await AssertDbUpdateFails(() => db.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Duplicate_payout_idempotency_and_round_user_reference_are_rejected()
    {
        await using var db = pg.CreateActivitiesContext();
        var session = await SeedRouletteAsync(db, withRound: true);
        var roundId = session.Round!.Id;

        db.RoulettePayouts.Add(new ActivityRoulettePayout { RouletteRoundId = roundId, DiscordUserId = "100", Amount = 25, IdempotencyKey = "payout-key" });
        db.RoulettePayouts.Add(new ActivityRoulettePayout { RouletteRoundId = roundId, DiscordUserId = "100", Amount = 25, IdempotencyKey = "payout-key-2" });

        await AssertDbUpdateFails(() => db.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Foreign_key_violation_is_enforced()
    {
        await using var db = pg.CreateActivitiesContext();

        db.RoulettePayouts.Add(new ActivityRoulettePayout { RouletteRoundId = Guid.NewGuid(), DiscordUserId = "100", Amount = 25, IdempotencyKey = "missing-round" });

        await AssertDbUpdateFails(() => db.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Decimal_precision_and_state_values_are_persisted()
    {
        await using var db = pg.CreateActivitiesContext();
        var session = await SeedRouletteAsync(db, withRound: true);

        db.RoulettePayouts.Add(new ActivityRoulettePayout
        {
            RouletteRoundId = session.Round!.Id,
            DiscordUserId = "101",
            Amount = 123.45m,
            IdempotencyKey = "precision-payout",
            Status = "RetryableFailed",
            NextAttemptAtUtc = DateTimeOffset.UtcNow.AddMinutes(1)
        });
        await db.SaveChangesAsync();

        var payout = await db.RoulettePayouts.AsNoTracking().SingleAsync(x => x.IdempotencyKey == "precision-payout");
        payout.Amount.Should().Be(123.45m);
        payout.Status.Should().Be("RetryableFailed");
        payout.NextAttemptAtUtc.Should().NotBeNull();
    }

    [DockerFact]
    public async Task Duplicate_wallet_reservation_idempotency_key_is_rejected()
    {
        await using var db = pg.CreatePlatformContext();
        var guild = await SeedGuildAsync(db);

        db.WalletReservations.Add(new WalletReservation { GuildId = guild.Id, DiscordUserId = "100", GameKey = "roulette", Amount = 10, IdempotencyKey = "reserve-key", Status = "Pending", ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(10) });
        db.WalletReservations.Add(new WalletReservation { GuildId = guild.Id, DiscordUserId = "100", GameKey = "roulette", Amount = 10, IdempotencyKey = "reserve-key", Status = "Pending", ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(10) });

        await AssertDbUpdateFails(() => db.SaveChangesAsync());
    }

    [DockerFact]
    public async Task Duplicate_wallet_credit_payout_reference_is_rejected()
    {
        await using var db = pg.CreatePlatformContext();
        var guild = await SeedGuildAsync(db);
        var payoutId = Guid.NewGuid();

        db.GameWalletTransactions.Add(new GameWalletTransaction { GuildId = guild.Id, UserDiscordId = "100", Amount = 10, Type = "WalletCredit", Reason = "payout", ReferenceId = payoutId });
        db.GameWalletTransactions.Add(new GameWalletTransaction { GuildId = guild.Id, UserDiscordId = "100", Amount = 10, Type = "WalletCredit", Reason = "payout", ReferenceId = payoutId });

        await AssertDbUpdateFails(() => db.SaveChangesAsync());
    }

    private static async Task<(ActivityRouletteGameSession Roulette, ActivityRouletteRound? Round)> SeedRouletteAsync(DiscordBot.Activities.Infrastructure.Data.ActivitiesDbContext db, bool withRound = false)
    {
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
            round = new ActivityRouletteRound { RouletteGameSession = roulette, RoundNumber = 1, SpinnerUserDiscordId = "100", IdempotencyKey = Guid.NewGuid().ToString("N") };
            roulette.Rounds.Add(round);
        }
        db.RouletteGameSessions.Add(roulette);
        await db.SaveChangesAsync();
        return (roulette, round);
    }

    private static async Task<Guild> SeedGuildAsync(DiscordBot.Infrastructure.Data.AppDbContext db)
    {
        var guild = new Guild { DiscordGuildId = RandomSnowflake(), Name = "Guild", OwnerDiscordUserId = "123456789012345678" };
        db.Guilds.Add(guild);
        await db.SaveChangesAsync();
        return guild;
    }

    private static string RandomSnowflake() => Random.Shared.NextInt64(100_000_000_000_000_000, 999_999_999_999_999_999).ToString();

    private static async Task AssertDbUpdateFails(Func<Task> action)
    {
        var act = async () => await action();
        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
