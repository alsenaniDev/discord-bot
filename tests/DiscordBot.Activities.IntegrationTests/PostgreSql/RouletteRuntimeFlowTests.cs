using ActivityGameSession = DiscordBot.Activities.Domain.Entities.GameSession;
using ActivityRouletteGameSession = DiscordBot.Activities.Domain.Entities.RouletteGameSession;
using ActivityRoulettePlayer = DiscordBot.Activities.Domain.Entities.RoulettePlayer;
using ActivitySessionEntity = DiscordBot.Activities.Domain.Entities.ActivitySession;
using DiscordBot.Activities.Application.Abstractions;
using DiscordBot.Activities.Application.Models;
using DiscordBot.Activities.Domain.Roulette;
using DiscordBot.Activities.Infrastructure.Data;
using DiscordBot.Activities.Infrastructure.Options;
using DiscordBot.Activities.Infrastructure.Platform;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DiscordBot.Activities.IntegrationTests.PostgreSql;

public sealed class RouletteRuntimeFlowTests(PostgreSqlFixture pg) : IClassFixture<PostgreSqlFixture>
{
    private const string GuildId = "1521518056852029440";
    private const string ChannelId = "1523998706331029574";
    private const string HostId = "941514638598746222";
    private const string OtherId = "687214635337777267";

    [DockerFact]
    public async Task Creating_room_queues_one_announcement_and_ack_updates_metadata()
    {
        await using var db = pg.CreateActivitiesContext();
        var service = Service(db);
        var hostId = UniqueSnowflake();

        var created = await service.CreateSessionAsync(new CreateRouletteSessionRequest { GuildDiscordId = GuildId, ChannelDiscordId = ChannelId, ActivityInstanceId = "instance-a", IdempotencyKey = $"create-announcement-{hostId}" }, User(hostId, "محمد"));

        created.Succeeded.Should().BeTrue();
        var pending = await service.GetPendingAnnouncementsAsync();
        pending.Should().ContainSingle(x => x.GameSessionId == created.Value!.GameSessionId);

        await service.AckAnnouncementAsync(created.Value!.GameSessionId, new AckRouletteAnnouncementRequest { Success = false, ErrorMessage = "Discord 429", RetryAfterSeconds = 600 });
        (await service.GetPendingAnnouncementsAsync()).Should().NotContain(x => x.GameSessionId == created.Value.GameSessionId, "failed announcements should wait for their retry time");

        var stored = await db.RouletteGameSessions.SingleAsync(x => x.GameSessionId == created.Value!.GameSessionId);
        stored.AnnouncementStatus.Should().Be("Failed");
        stored.AnnouncementLastError.Should().Be("Discord 429");
        stored.AnnouncementNextAttemptAtUtc = DateTimeOffset.UtcNow.AddSeconds(-1);
        await db.SaveChangesAsync();

        (await service.GetPendingAnnouncementsAsync()).Should().ContainSingle(x => x.GameSessionId == created.Value.GameSessionId);
        await service.AckAnnouncementAsync(created.Value.GameSessionId, new AckRouletteAnnouncementRequest { Success = true, MessageDiscordId = "123456789012345678" });

        stored = await db.RouletteGameSessions.AsNoTracking().SingleAsync(x => x.GameSessionId == created.Value.GameSessionId);
        stored.AnnouncementStatus.Should().Be("Posted");
        stored.DiscordAnnouncementMessageId.Should().Be("123456789012345678");
        stored.AnnouncementCreatedAtUtc.Should().NotBeNull();
    }

    [DockerFact]
    public async Task Prepared_join_intent_is_consumed_once()
    {
        await using var setup = pg.CreateActivitiesContext();
        var seed = await SeedWaitingRoomAsync(setup);
        await using var db = pg.CreateActivitiesContext();
        var service = Service(db);

        var prepared = await service.PrepareJoinAsync(seed.GameSessionId, new PrepareRouletteJoinRequest { GuildDiscordId = GuildId, ChannelDiscordId = ChannelId, UserDiscordId = OtherId, Username = "نايف" });
        var consumed = await service.ConsumePendingIntentAsync(GuildId, ChannelId, OtherId);
        var consumedAgain = await service.ConsumePendingIntentAsync(GuildId, ChannelId, OtherId);

        prepared.Succeeded.Should().BeTrue();
        consumed.Value.Should().NotBeNull();
        consumed.Value!.RoomId.Should().Be(seed.GameSessionId);
        consumedAgain.Value.Should().BeNull();
    }

    [DockerFact]
    public async Task Valid_second_player_join_succeeds_from_consumed_intent()
    {
        await using var setup = pg.CreateActivitiesContext();
        var seed = await SeedWaitingRoomAsync(setup);
        await using var db = pg.CreateActivitiesContext();
        var service = Service(db);

        var prepared = await service.PrepareJoinAsync(seed.GameSessionId, new PrepareRouletteJoinRequest { GuildDiscordId = GuildId, ChannelDiscordId = ChannelId, UserDiscordId = OtherId, Username = "نايف" });
        var intent = await service.ConsumePendingIntentAsync(GuildId, ChannelId, OtherId);
        var joined = await service.JoinSessionAsync(intent.Value!.GameSessionId, Scope(), User(OtherId, "نايف"));

        prepared.Succeeded.Should().BeTrue();
        intent.Succeeded.Should().BeTrue();
        joined.Succeeded.Should().BeTrue();
        joined.Value!.Players.Should().Contain(x => x.UserDiscordId == OtherId);
    }

    [DockerFact]
    public async Task Concurrent_duplicate_join_returns_existing_membership_not_500()
    {
        await using var setup = pg.CreateActivitiesContext();
        var seed = await SeedWaitingRoomAsync(setup);
        await using var firstDb = pg.CreateActivitiesContext();
        await using var secondDb = pg.CreateActivitiesContext();
        var first = Service(firstDb);
        var second = Service(secondDb);

        var results = await Task.WhenAll(
            first.JoinSessionAsync(seed.GameSessionId, Scope(), User(OtherId, "نايف")),
            second.JoinSessionAsync(seed.GameSessionId, Scope(), User(OtherId, "نايف")));

        results.Should().OnlyContain(x => x.Succeeded);
        await using var verify = pg.CreateActivitiesContext();
        var joinedCount = await verify.RoulettePlayers.CountAsync(x => x.RouletteGameSessionId == seed.Id && x.DiscordUserId == OtherId);
        joinedCount.Should().Be(1);
    }

    [DockerFact]
    public async Task Only_player_leaves_room_without_500_and_duplicate_leave_is_idempotent()
    {
        await using var setup = pg.CreateActivitiesContext();
        var seed = await SeedWaitingRoomAsync(setup);
        await using var db = pg.CreateActivitiesContext();
        var service = Service(db);
        var request = Scope();

        var first = await service.LeaveSessionAsync(seed.GameSessionId, request, HostId);
        var duplicate = await service.LeaveSessionAsync(seed.GameSessionId, request, HostId);

        first.Succeeded.Should().BeTrue();
        first.Value!.Status.Should().Be(RouletteRuntimeStates.Cancelled);
        first.Value.Players.Should().BeEmpty();
        duplicate.Succeeded.Should().BeTrue();
        duplicate.Value!.Status.Should().Be(RouletteRuntimeStates.Cancelled);
    }

    [DockerFact]
    public async Task Owner_leave_transfers_host_to_oldest_remaining_player()
    {
        await using var setup = pg.CreateActivitiesContext();
        var seed = await SeedWaitingRoomAsync(setup, includeOtherPlayer: true);
        await using var db = pg.CreateActivitiesContext();
        var service = Service(db);

        var result = await service.LeaveSessionAsync(seed.GameSessionId, Scope(), HostId);

        result.Succeeded.Should().BeTrue();
        result.Value!.Status.Should().Be(RouletteRuntimeStates.WaitingForPlayers);
        result.Value.HostUserDiscordId.Should().Be(OtherId);
        result.Value.Players.Should().ContainSingle();
        result.Value.Players.Single().IsHost.Should().BeTrue();
    }

    [DockerFact]
    public async Task Host_can_start_valid_room_and_round_is_persisted()
    {
        await using var setup = pg.CreateActivitiesContext();
        var seed = await SeedWaitingRoomAsync(setup, includeOtherPlayer: true);
        await using var db = pg.CreateActivitiesContext();
        var service = Service(db);

        var result = await service.StartSessionAsync(seed.GameSessionId, Scope(), HostId);

        result.Succeeded.Should().BeTrue();
        result.Value!.Status.Should().Be(RouletteRuntimeStates.BettingOpen);
        result.Value.CurrentTurnUserDiscordId.Should().Be(HostId);
        var roundCount = await db.RouletteRounds.CountAsync(x => x.RouletteGameSessionId == seed.Id && x.RoundNumber == 1);
        roundCount.Should().Be(1);
    }

    [DockerFact]
    public async Task Non_host_cannot_start_room()
    {
        await using var setup = pg.CreateActivitiesContext();
        var seed = await SeedWaitingRoomAsync(setup, includeOtherPlayer: true);
        await using var db = pg.CreateActivitiesContext();
        var service = Service(db);

        var result = await service.StartSessionAsync(seed.GameSessionId, Scope(), OtherId);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Code.Should().Be("roulette_only_host_can_start");
    }

    [DockerFact]
    public async Task Transferred_host_can_start_and_old_host_cannot_start_after_leaving()
    {
        await using var setup = pg.CreateActivitiesContext();
        var seed = await SeedWaitingRoomAsync(setup, includeOtherPlayer: true);
        await using var db = pg.CreateActivitiesContext();
        var service = Service(db);

        var leave = await service.LeaveSessionAsync(seed.GameSessionId, Scope(), HostId);
        var oldHostStart = await service.StartSessionAsync(seed.GameSessionId, Scope(), HostId);
        var newHostStart = await service.StartSessionAsync(seed.GameSessionId, Scope(), OtherId);

        leave.Succeeded.Should().BeTrue();
        leave.Value!.HostUserDiscordId.Should().Be(OtherId);
        oldHostStart.Succeeded.Should().BeFalse();
        oldHostStart.Code.Should().Be("roulette_player_not_in_session");
        newHostStart.Succeeded.Should().BeFalse("only one player remains and min players is 2");
        newHostStart.Code.Should().Be("roulette_not_enough_players");
    }

    [DockerFact]
    public async Task Transferred_host_can_start_when_enough_players_remain()
    {
        await using var setup = pg.CreateActivitiesContext();
        var thirdId = UniqueSnowflake();
        var seed = await SeedWaitingRoomAsync(setup, includeOtherPlayer: true, thirdPlayerId: thirdId);
        await using var db = pg.CreateActivitiesContext();
        var service = Service(db);

        var leave = await service.LeaveSessionAsync(seed.GameSessionId, Scope(), HostId);
        var start = await service.StartSessionAsync(seed.GameSessionId, Scope(), OtherId);

        leave.Value!.HostUserDiscordId.Should().Be(OtherId);
        leave.Value.Players.Count(x => x.IsHost).Should().Be(1);
        start.Succeeded.Should().BeTrue();
        start.Value!.Status.Should().Be(RouletteRuntimeStates.BettingOpen);
        start.Value.HostUserDiscordId.Should().Be(OtherId);
    }

    [DockerFact]
    public async Task Duplicate_start_is_idempotent_and_creates_one_round()
    {
        await using var setup = pg.CreateActivitiesContext();
        var seed = await SeedWaitingRoomAsync(setup, includeOtherPlayer: true);
        await using var db = pg.CreateActivitiesContext();
        var service = Service(db);

        var first = await service.StartSessionAsync(seed.GameSessionId, Scope(), HostId);
        var second = await service.StartSessionAsync(seed.GameSessionId, Scope(), HostId);

        first.Succeeded.Should().BeTrue();
        second.Succeeded.Should().BeTrue();
        var roundCount = await db.RouletteRounds.CountAsync(x => x.RouletteGameSessionId == seed.Id && x.RoundNumber == 1);
        roundCount.Should().Be(1);
    }

    [DockerFact]
    public async Task Concurrent_start_requests_create_one_round_without_500()
    {
        await using var setup = pg.CreateActivitiesContext();
        var seed = await SeedWaitingRoomAsync(setup, includeOtherPlayer: true);
        await using var firstDb = pg.CreateActivitiesContext();
        await using var secondDb = pg.CreateActivitiesContext();
        var first = Service(firstDb);
        var second = Service(secondDb);

        var results = await Task.WhenAll(
            first.StartSessionAsync(seed.GameSessionId, Scope(), HostId),
            second.StartSessionAsync(seed.GameSessionId, Scope(), HostId));

        results.Should().OnlyContain(x => x.Succeeded);
        await using var verify = pg.CreateActivitiesContext();
        var roundCount = await verify.RouletteRounds.CountAsync(x => x.RouletteGameSessionId == seed.Id && x.RoundNumber == 1);
        roundCount.Should().Be(1);
    }

    [DockerFact]
    public async Task Insufficient_players_returns_structured_error_not_500()
    {
        await using var setup = pg.CreateActivitiesContext();
        var seed = await SeedWaitingRoomAsync(setup);
        await using var db = pg.CreateActivitiesContext();
        var service = Service(db);

        var result = await service.StartSessionAsync(seed.GameSessionId, Scope(), HostId);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Code.Should().Be("roulette_not_enough_players");
    }

    [DockerFact]
    public async Task Previous_day_waiting_room_is_expired_and_not_returned_by_my_active()
    {
        await using var setup = pg.CreateActivitiesContext();
        var hostId = UniqueSnowflake();
        var otherId = UniqueSnowflake();
        var seed = await SeedWaitingRoomAsync(setup, includeOtherPlayer: true, hostId: hostId, otherId: otherId);
        await setup.RouletteGameSessions.Where(x => x.Id == seed.Id).ExecuteUpdateAsync(setters => setters
            .SetProperty(x => x.ExpiresAtUtc, DateTimeOffset.UtcNow.AddDays(-1))
            .SetProperty(x => x.UpdatedAtUtc, DateTimeOffset.UtcNow.AddDays(-1)));
        await setup.GameSessions.Where(x => x.Id == seed.GameSessionId).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UpdatedAtUtc, DateTimeOffset.UtcNow.AddDays(-1)));
        await using var db = pg.CreateActivitiesContext();
        var service = Service(db);

        var result = await service.GetMyActiveSessionAsync(GuildId, ChannelId, hostId);

        result.Succeeded.Should().BeTrue();
        result.Value!.HasRoom.Should().BeFalse();
        result.Value.ResumeAllowed.Should().BeFalse();
        var stored = await db.RouletteGameSessions.AsNoTracking().SingleAsync(x => x.Id == seed.Id);
        stored.Status.Should().Be(RouletteRuntimeStates.Expired);
    }

    [DockerFact]
    public async Task Inactive_in_progress_room_is_abandoned_and_not_resumed()
    {
        await using var setup = pg.CreateActivitiesContext();
        var hostId = UniqueSnowflake();
        var otherId = UniqueSnowflake();
        var seed = await SeedWaitingRoomAsync(setup, includeOtherPlayer: true, hostId: hostId, otherId: otherId);
        await setup.RouletteGameSessions.Where(x => x.Id == seed.Id).ExecuteUpdateAsync(setters => setters
            .SetProperty(x => x.Status, RouletteRuntimeStates.BettingOpen)
            .SetProperty(x => x.StartedAtUtc, DateTimeOffset.UtcNow.AddDays(-1))
            .SetProperty(x => x.UpdatedAtUtc, DateTimeOffset.UtcNow.AddDays(-1)));
        await setup.GameSessions.Where(x => x.Id == seed.GameSessionId).ExecuteUpdateAsync(setters => setters
            .SetProperty(x => x.Status, RouletteRuntimeStates.BettingOpen)
            .SetProperty(x => x.StartedAtUtc, DateTimeOffset.UtcNow.AddDays(-1))
            .SetProperty(x => x.UpdatedAtUtc, DateTimeOffset.UtcNow.AddDays(-1)));
        await using var db = pg.CreateActivitiesContext();
        var service = Service(db, new RouletteRuntimeOptions { InProgressAbandonmentMinutes = 60, ResumeWindowMinutes = 120 });

        var result = await service.GetMyActiveSessionAsync(GuildId, ChannelId, hostId);

        result.Value!.HasRoom.Should().BeFalse();
        var stored = await db.RouletteGameSessions.AsNoTracking().SingleAsync(x => x.Id == seed.Id);
        stored.Status.Should().Be(RouletteRuntimeStates.Abandoned);
    }

    [DockerFact]
    public async Task Expired_room_start_returns_structured_error_not_500()
    {
        await using var setup = pg.CreateActivitiesContext();
        var seed = await SeedWaitingRoomAsync(setup, includeOtherPlayer: true);
        await setup.RouletteGameSessions.Where(x => x.Id == seed.Id).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ExpiresAtUtc, DateTimeOffset.UtcNow.AddMinutes(-1)));
        await using var db = pg.CreateActivitiesContext();
        var service = Service(db);

        var result = await service.StartSessionAsync(seed.GameSessionId, Scope(), HostId);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Code.Should().Be("roulette_session_not_startable");
    }

    [DockerFact]
    public async Task Unauthorized_leave_returns_structured_404_not_500()
    {
        await using var setup = pg.CreateActivitiesContext();
        var seed = await SeedWaitingRoomAsync(setup);
        await using var db = pg.CreateActivitiesContext();
        var service = Service(db);

        var result = await service.LeaveSessionAsync(seed.GameSessionId, Scope(), OtherId);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Code.Should().Be("roulette_player_not_in_session");
    }

    private static RouletteScopeRequest Scope() => new() { GuildDiscordId = GuildId, ChannelDiscordId = ChannelId, ActivityInstanceId = "instance-a" };

    private static string UniqueSnowflake() => (900_000_000_000_000_000UL + (ulong)Random.Shared.Next(1_000_000, 999_999_999)).ToString();

    private static TrustedDiscordUser User(string userId, string username) => new()
    {
        DiscordUserId = userId,
        Username = username,
        DiscordGuildId = GuildId,
        DiscordChannelId = ChannelId,
        ActivityInstanceId = "instance-a"
    };

    private static RouletteRuntimeService Service(ActivitiesDbContext db, RouletteRuntimeOptions? options = null) => new(db, new FakePlatformApiClient(), new FakeRealtimePublisher(), Options.Create(options ?? new RouletteRuntimeOptions()), NullLogger<RouletteRuntimeService>.Instance);

    private static async Task<ActivityRouletteGameSession> SeedWaitingRoomAsync(ActivitiesDbContext db, bool includeOtherPlayer = false, string? thirdPlayerId = null, string hostId = HostId, string otherId = OtherId)
    {
        var activity = new ActivitySessionEntity
        {
            DiscordUserId = hostId,
            Username = "محمد",
            DiscordGuildId = GuildId,
            DiscordChannelId = ChannelId,
            DiscordActivityInstanceId = "instance-a",
            GameKey = "roulette",
            GameVersion = "1.0.0",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1)
        };
        var game = new ActivityGameSession { ActivitySession = activity, GameKey = "roulette", GameVersion = "1.0.0", DiscordGuildId = GuildId, DiscordChannelId = ChannelId, Status = RouletteRuntimeStates.WaitingForPlayers };
        var roulette = new ActivityRouletteGameSession
        {
            GameSession = game,
            HostUserDiscordId = hostId,
            HostUsername = "محمد",
            Status = RouletteRuntimeStates.WaitingForPlayers,
            MinPlayers = 2,
            MaxPlayers = 6,
            WinnerCoins = 100,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
            DiscordAnnouncementChannelId = ChannelId,
            AnnouncementStatus = "Pending",
            AnnouncementRequestedAtUtc = DateTimeOffset.UtcNow,
            AnnouncementNextAttemptAtUtc = DateTimeOffset.UtcNow
        };
        roulette.Players.Add(new ActivityRoulettePlayer { DiscordUserId = hostId, Username = "محمد", IsHost = true, Position = 1, JoinedAtUtc = DateTimeOffset.UtcNow.AddSeconds(-10) });
        if (includeOtherPlayer) roulette.Players.Add(new ActivityRoulettePlayer { DiscordUserId = otherId, Username = "نايف", Position = 2, JoinedAtUtc = DateTimeOffset.UtcNow });
        if (!string.IsNullOrWhiteSpace(thirdPlayerId)) roulette.Players.Add(new ActivityRoulettePlayer { DiscordUserId = thirdPlayerId, Username = "سعود", Position = 3, JoinedAtUtc = DateTimeOffset.UtcNow.AddSeconds(1) });
        db.RouletteGameSessions.Add(roulette);
        await db.SaveChangesAsync();
        return roulette;
    }

    private sealed class FakePlatformApiClient : IPlatformApiClient
    {
        public Task<GameAccessResult> ValidateGameAccessAsync(ValidateGameAccessRequest request, CancellationToken cancellationToken = default) => Task.FromResult(new GameAccessResult { Allowed = true, GameKey = request.GameKey, GameVersion = "1.0.0", SupportsWallet = true, RouletteSettings = new RouletteSettingsSnapshot() });
        public Task<WalletReservationResult> ReserveWalletAsync(ReserveWalletRequest request, CancellationToken cancellationToken = default) => Task.FromResult(new WalletReservationResult { Succeeded = true, ReservationId = Guid.NewGuid().ToString("N"), Status = "Pending" });
        public Task CommitWalletReservationAsync(string reservationId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ReleaseWalletReservationAsync(string reservationId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<WalletCreditResult> CreditWalletAsync(WalletCreditRequest request, CancellationToken cancellationToken = default) => Task.FromResult(new WalletCreditResult { Succeeded = true, Status = "Paid", Amount = request.Amount });
    }

    private sealed class FakeRealtimePublisher : IRouletteRealtimePublisher
    {
        public Task PublishAsync(RouletteRealtimeEvent evt, CancellationToken ct = default) => Task.CompletedTask;
    }

}
