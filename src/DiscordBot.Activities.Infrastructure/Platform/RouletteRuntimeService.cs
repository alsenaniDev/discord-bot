using System.Security.Cryptography;
using System.Text.Json;
using DiscordBot.Activities.Application.Abstractions;
using DiscordBot.Activities.Application.Models;
using DiscordBot.Activities.Domain.Entities;
using DiscordBot.Activities.Domain.Roulette;
using DiscordBot.Activities.Infrastructure.Data;
using DiscordBot.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Activities.Infrastructure.Platform;

public class RouletteRuntimeService(
    ActivitiesDbContext db,
    IPlatformApiClient platform,
    IRouletteRealtimePublisher realtime,
    ILogger<RouletteRuntimeService> logger) : IRouletteRuntimeService
{
    private const string GameKey = "roulette";

    public async Task<OperationResult<RouletteSessionDto>> CreateSessionAsync(CreateRouletteSessionRequest request, TrustedDiscordUser user, CancellationToken ct = default)
    {
        var access = await ValidateAccessAsync(request.GuildDiscordId, request.ChannelDiscordId, user.DiscordUserId, ct);
        if (!access.Succeeded) return OperationResult<RouletteSessionDto>.Fail(access.Error!, access.StatusCode);

        var now = DateTimeOffset.UtcNow;
        var settings = access.Value!.RouletteSettings ?? new RouletteSettingsSnapshot();
        var idempotencyKey = Idempotency(request.IdempotencyKey, "create", user.DiscordUserId, request.GuildDiscordId, request.ChannelDiscordId);

        var existing = await db.GameEvents.AsNoTracking()
            .Where(x => x.GameKey == GameKey && x.IdempotencyKey == idempotencyKey && x.EventType == "RouletteSessionCreateRequested")
            .Select(x => x.GameSessionId)
            .FirstOrDefaultAsync(ct);
        if (existing != Guid.Empty)
        {
            var current = await LoadByGameSessionAsync(existing, ct);
            if (current is not null) return OperationResult<RouletteSessionDto>.Ok(Map(current, user.DiscordUserId));
        }

        if (await HasActiveHostSessionAsync(request.GuildDiscordId, request.ChannelDiscordId, user.DiscordUserId, now, ct))
            return OperationResult<RouletteSessionDto>.Fail("لديك غرفة روليت نشطة بالفعل.", 409);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var activity = new ActivitySession
        {
            DiscordUserId = user.DiscordUserId,
            Username = CleanUsername(user.Username, user.DiscordUserId),
            AvatarUrl = LimitNullable(user.AvatarUrl, 512),
            DiscordGuildId = request.GuildDiscordId,
            DiscordChannelId = request.ChannelDiscordId,
            DiscordActivityInstanceId = LimitNullable(request.ActivityInstanceId, 128),
            GameKey = GameKey,
            GameVersion = access.Value.GameVersion,
            PlatformGameVersionId = access.Value.PlatformGameVersionId,
            Mode = access.Value.Mode,
            Status = "Active",
            ExpiresAtUtc = now.AddHours(2),
            LastSeenAtUtc = now
        };
        activity.Players.Add(new ActivityPlayer
        {
            DiscordUserId = user.DiscordUserId,
            Username = CleanUsername(user.Username, user.DiscordUserId),
            AvatarUrl = LimitNullable(user.AvatarUrl, 512),
            ConnectionStatus = "Online",
            JoinedAtUtc = now,
            LastSeenAtUtc = now
        });

        var game = new GameSession
        {
            ActivitySession = activity,
            GameKey = GameKey,
            GameVersion = access.Value.GameVersion,
            DiscordGuildId = request.GuildDiscordId,
            DiscordChannelId = request.ChannelDiscordId,
            Status = RouletteRuntimeStates.WaitingForPlayers
        };

        var cleanName = CleanUsername(user.Username, user.DiscordUserId);
        var roulette = new RouletteGameSession
        {
            GameSession = game,
            HostUserDiscordId = user.DiscordUserId,
            HostUsername = cleanName,
            Status = RouletteRuntimeStates.WaitingForPlayers,
            MinPlayers = Math.Clamp(settings.MinPlayers, 2, 10),
            MaxPlayers = Math.Clamp(settings.MaxPlayers, Math.Clamp(settings.MinPlayers, 2, 10), 10),
            WinnerCoins = Math.Max(0, settings.WinnerCoins),
            SecondPlaceCoins = Math.Max(0, settings.SecondPlaceCoins),
            ParticipationCoins = Math.Max(0, settings.ParticipationCoins),
            PendingActionStatus = "None",
            ExpiresAtUtc = now.AddSeconds(Math.Clamp(settings.JoinWindowSeconds, 30, 300))
        };
        roulette.Players.Add(new RoulettePlayer
        {
            DiscordUserId = user.DiscordUserId,
            Username = cleanName,
            DisplayName = cleanName,
            AvatarUrl = LimitNullable(user.AvatarUrl, 512),
            IsHost = true,
            Position = 1,
            JoinedAtUtc = now
        });

        db.ActivitySessions.Add(activity);
        db.GameSessions.Add(game);
        db.RouletteGameSessions.Add(roulette);
        AddEvent(game, 0, user.DiscordUserId, null, "RouletteSessionCreateRequested", idempotencyKey, new { message = "تم إنشاء غرفة الروليت." });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var dto = Map(roulette, user.DiscordUserId);
        await PublishAsync(dto.GameSessionId, "RouletteSessionUpdated", dto, ct);
        logger.LogInformation("Activities Roulette session {GameSessionId} created for guild {GuildId}, channel {ChannelId}, user {UserId}.", dto.GameSessionId, request.GuildDiscordId, request.ChannelDiscordId, user.DiscordUserId);
        return OperationResult<RouletteSessionDto>.Ok(dto);
    }

    public async Task<OperationResult<IReadOnlyList<RouletteSessionDto>>> GetOpenSessionsAsync(string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default)
    {
        var access = await ValidateAccessAsync(guildDiscordId, channelDiscordId, userDiscordId, ct);
        if (!access.Succeeded) return OperationResult<IReadOnlyList<RouletteSessionDto>>.Fail(access.Error!, access.StatusCode);

        var now = DateTimeOffset.UtcNow;
        var sessions = await db.RouletteGameSessions.AsNoTracking()
            .Include(x => x.GameSession)
            .Include(x => x.Players)
            .Where(x => x.GameSession.DiscordGuildId == guildDiscordId
                && x.GameSession.DiscordChannelId == channelDiscordId
                && x.Status == RouletteRuntimeStates.WaitingForPlayers
                && x.ExpiresAtUtc > now
                && x.Players.Count < x.MaxPlayers)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(30)
            .ToListAsync(ct);
        return OperationResult<IReadOnlyList<RouletteSessionDto>>.Ok(sessions.Select(x => Map(x, userDiscordId)).ToList());
    }

    public async Task<OperationResult<MyActiveRouletteSessionDto>> GetMyActiveSessionAsync(string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var session = await db.RouletteGameSessions.AsNoTracking()
            .Include(x => x.GameSession)
            .Include(x => x.Players)
            .Where(x => x.GameSession.DiscordGuildId == guildDiscordId
                && x.GameSession.DiscordChannelId == channelDiscordId
                && ((x.Status == RouletteRuntimeStates.WaitingForPlayers && x.ExpiresAtUtc > now) || x.Status == RouletteRuntimeStates.BettingOpen)
                && x.Players.Any(p => p.DiscordUserId == userDiscordId))
            .OrderByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefaultAsync(ct);

        return OperationResult<MyActiveRouletteSessionDto>.Ok(session is null
            ? new MyActiveRouletteSessionDto { HasRoom = false }
            : new MyActiveRouletteSessionDto { HasRoom = true, RoomId = session.GameSessionId, GameSessionId = session.GameSessionId, Status = session.Status, IsHost = session.HostUserDiscordId == userDiscordId });
    }

    public async Task<OperationResult<RouletteSessionDto>> GetSessionAsync(Guid gameSessionId, string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default)
    {
        var session = await LoadByGameSessionAsync(gameSessionId, ct);
        var scope = ValidateScope(session, guildDiscordId, channelDiscordId);
        if (scope is not null) return OperationResult<RouletteSessionDto>.Fail(scope.Value.Message, scope.Value.Code);
        await ExpireIfNeededAsync(session!, ct);
        return OperationResult<RouletteSessionDto>.Ok(Map(session!, userDiscordId));
    }

    public async Task<OperationResult<RouletteSessionDto>> JoinSessionAsync(Guid gameSessionId, RouletteScopeRequest request, TrustedDiscordUser user, CancellationToken ct = default)
    {
        var access = await ValidateAccessAsync(request.GuildDiscordId, request.ChannelDiscordId, user.DiscordUserId, ct);
        if (!access.Succeeded) return OperationResult<RouletteSessionDto>.Fail(access.Error!, access.StatusCode);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var session = await LoadByGameSessionAsync(gameSessionId, ct);
        var scope = ValidateScope(session, request.GuildDiscordId, request.ChannelDiscordId, request.ActivityInstanceId);
        if (scope is not null) return OperationResult<RouletteSessionDto>.Fail(scope.Value.Message, scope.Value.Code);
        var existing = session!.Players.FirstOrDefault(x => x.DiscordUserId == user.DiscordUserId);
        if (existing is not null && session.Status is RouletteRuntimeStates.WaitingForPlayers or RouletteRuntimeStates.BettingOpen)
        {
            await transaction.CommitAsync(ct);
            return OperationResult<RouletteSessionDto>.Ok(Map(session, user.DiscordUserId));
        }
        if (!RouletteRuntimeStates.IsOpenForJoin(session.Status)) return OperationResult<RouletteSessionDto>.Fail("هذه الجولة لم تعد متاحة للانضمام.", 409);
        if (session.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            Transition(session, RouletteRuntimeStates.Expired);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return OperationResult<RouletteSessionDto>.Fail("انتهت مدة الانضمام لهذه الجولة.", 410);
        }
        if (session.Players.Count >= session.MaxPlayers) return OperationResult<RouletteSessionDto>.Fail("اكتمل عدد اللاعبين في هذه الجولة.", 409);

        var cleanName = CleanUsername(user.Username, user.DiscordUserId);
        session.Players.Add(new RoulettePlayer
        {
            DiscordUserId = user.DiscordUserId,
            Username = cleanName,
            DisplayName = cleanName,
            AvatarUrl = LimitNullable(user.AvatarUrl, 512),
            Position = session.Players.Count + 1,
            JoinedAtUtc = DateTimeOffset.UtcNow
        });
        AddEvent(session.GameSession, 0, user.DiscordUserId, null, "RoulettePlayerJoined", Idempotency(request.IdempotencyKey, "join", gameSessionId, user.DiscordUserId), new { message = "انضم لاعب للغرفة." });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var dto = Map(session, user.DiscordUserId);
        await PublishAsync(dto.GameSessionId, "RoulettePlayerJoined", dto, ct);
        return OperationResult<RouletteSessionDto>.Ok(dto);
    }

    public async Task<OperationResult<RouletteSessionDto>> LeaveSessionAsync(Guid gameSessionId, RouletteScopeRequest request, string userDiscordId, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var session = await LoadByGameSessionAsync(gameSessionId, ct);
        var scope = ValidateScope(session, request.GuildDiscordId, request.ChannelDiscordId, request.ActivityInstanceId);
        if (scope is not null) return OperationResult<RouletteSessionDto>.Fail(scope.Value.Message, scope.Value.Code);
        if (!RouletteRuntimeStates.IsOpenForJoin(session!.Status)) return OperationResult<RouletteSessionDto>.Fail("لا يمكن مغادرة الجولة بعد بدء اللعبة.", 409);
        var player = session.Players.FirstOrDefault(x => x.DiscordUserId == userDiscordId);
        if (player is null) return OperationResult<RouletteSessionDto>.Fail("أنت غير منضم لهذه الجولة.", 404);

        db.RoulettePlayers.Remove(player);
        session.Players.Remove(player);
        AddEvent(session.GameSession, 0, userDiscordId, null, "RoulettePlayerLeft", Idempotency(request.IdempotencyKey, "leave", gameSessionId, userDiscordId), new { message = "غادر لاعب الغرفة." });
        if (player.IsHost)
        {
            var next = session.Players.OrderBy(x => x.JoinedAtUtc).FirstOrDefault();
            if (next is null)
            {
                Transition(session, RouletteRuntimeStates.Cancelled);
            }
            else
            {
                next.IsHost = true;
                session.HostUserDiscordId = next.DiscordUserId;
                session.HostUsername = next.Username;
            }
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        var dto = Map(session, userDiscordId);
        await PublishAsync(dto.GameSessionId, "RoulettePlayerLeft", dto, ct);
        return OperationResult<RouletteSessionDto>.Ok(dto);
    }

    public async Task<OperationResult<RouletteSessionDto>> StartSessionAsync(Guid gameSessionId, RouletteScopeRequest request, string userDiscordId, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var session = await LoadByGameSessionAsync(gameSessionId, ct);
        var scope = ValidateScope(session, request.GuildDiscordId, request.ChannelDiscordId, request.ActivityInstanceId);
        if (scope is not null) return OperationResult<RouletteSessionDto>.Fail(scope.Value.Message, scope.Value.Code);
        if (session!.HostUserDiscordId != userDiscordId) return OperationResult<RouletteSessionDto>.Fail("فقط صاحب الغرفة يقدر يبدأ اللعبة.", 403);
        if (!RouletteRuntimeStates.IsOpenForJoin(session.Status)) return OperationResult<RouletteSessionDto>.Fail("هذه الجولة بدأت أو انتهت مسبقًا.", 409);
        if (session.ExpiresAtUtc <= DateTimeOffset.UtcNow) return OperationResult<RouletteSessionDto>.Fail("انتهت مدة الانضمام لهذه الجولة.", 410);
        if (session.Players.Count < session.MinPlayers) return OperationResult<RouletteSessionDto>.Fail($"تحتاج {session.MinPlayers} لاعبين على الأقل لبدء اللعبة.", 409);
        Transition(session, RouletteRuntimeStates.BettingOpen);
        session.GameSession.StartedAtUtc = DateTimeOffset.UtcNow;
        session.StartedAtUtc = DateTimeOffset.UtcNow;
        session.CurrentRound = 1;
        session.CurrentTurnUserDiscordId = session.Players.OrderBy(x => x.Position).First(x => x.IsAlive).DiscordUserId;
        session.PendingActionStatus = "None";
        AddEvent(session.GameSession, 1, userDiscordId, null, "RouletteRoundStarted", Idempotency(request.IdempotencyKey, "start", gameSessionId, userDiscordId), new { message = "بدأت لعبة الروليت." });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        var dto = Map(session, userDiscordId);
        await PublishAsync(dto.GameSessionId, "RouletteRoundStarted", dto, ct);
        return OperationResult<RouletteSessionDto>.Ok(dto);
    }

    public async Task<OperationResult<RouletteSpinResultDto>> SpinAsync(Guid gameSessionId, RouletteScopeRequest request, string userDiscordId, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var session = await LoadByGameSessionAsync(gameSessionId, ct);
        var scope = ValidateScope(session, request.GuildDiscordId, request.ChannelDiscordId, request.ActivityInstanceId);
        if (scope is not null) return OperationResult<RouletteSpinResultDto>.Fail(scope.Value.Message, scope.Value.Code);
        if (session!.Status != RouletteRuntimeStates.BettingOpen) return OperationResult<RouletteSpinResultDto>.Fail("اللعبة غير جاهزة للتدوير.", 409);
        if (session.PendingActionStatus is "WaitingForPowerUp" or "AutoResolved") return OperationResult<RouletteSpinResultDto>.Fail("يوجد إجراء معلق، انتظر حتى يتم تنفيذ نتيجة العجلة.", 409);
        if (session.CurrentTurnUserDiscordId != userDiscordId) return OperationResult<RouletteSpinResultDto>.Fail("ليس دورك الآن.", 403);
        var alive = session.Players.Where(x => x.IsAlive).OrderBy(x => x.Position).ToList();
        if (alive.Count <= 1) return OperationResult<RouletteSpinResultDto>.Fail("انتهت هذه الجولة مسبقًا.", 409);

        var spinner = alive.First(x => x.DiscordUserId == userDiscordId);
        var target = PickTarget(alive, userDiscordId);
        var selectedIndex = alive.FindIndex(x => x.DiscordUserId == target.DiscordUserId);
        var now = DateTimeOffset.UtcNow;
        session.PendingTargetUserDiscordId = target.DiscordUserId;
        session.PendingActionStatus = "AutoResolved";
        session.PendingActionExpiresAtUtc = now;
        session.LastSpinResultJson = JsonSerializer.Serialize(new RouletteSpinResultInfoDto
        {
            SpinnerUserDiscordId = spinner.DiscordUserId,
            SpinnerUsername = spinner.Username,
            SpinnerAvatarUrl = spinner.AvatarUrl,
            TargetUserDiscordId = target.DiscordUserId,
            TargetUsername = target.Username,
            TargetAvatarUrl = target.AvatarUrl,
            SelectedIndex = selectedIndex,
            ResultType = "PendingElimination",
            CreatedAt = now
        });
        session.Rounds.Add(new RouletteRound
        {
            RoundNumber = session.CurrentRound,
            Status = "ResultGenerated",
            SpinnerUserDiscordId = spinner.DiscordUserId,
            TargetUserDiscordId = target.DiscordUserId,
            SelectedIndex = selectedIndex,
            ResultJson = session.LastSpinResultJson,
            IdempotencyKey = Idempotency(request.IdempotencyKey, "spin", gameSessionId, userDiscordId, session.CurrentRound),
            StartedAtUtc = now,
            CompletedAtUtc = now
        });
        AddEvent(session.GameSession, session.CurrentRound, spinner.DiscordUserId, target.DiscordUserId, "RouletteRoundResult", Idempotency(null, "event-spin", gameSessionId, userDiscordId, session.CurrentRound), new { spinnerUsername = spinner.Username, targetUsername = target.Username, selectedIndex, message = $"🎡 العجلة اختارت {target.Username}. لا يملك خصائص متاحة في runtime الجديد." });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var result = new RouletteSpinResultDto { Room = Map(session, userDiscordId), TargetPlayer = MapPlayer(target), AlivePlayers = alive.Select(MapPlayer).ToList(), SelectedIndex = selectedIndex, TargetHasUsablePowerUps = false };
        await PublishAsync(result.Room.GameSessionId, "RouletteRoundResult", result, ct);
        return OperationResult<RouletteSpinResultDto>.Ok(result);
    }

    public async Task<OperationResult<RouletteSessionDto>> ResolvePendingActionAsync(Guid gameSessionId, RouletteScopeRequest request, string userDiscordId, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var session = await LoadByGameSessionAsync(gameSessionId, ct);
        var scope = ValidateScope(session, request.GuildDiscordId, request.ChannelDiscordId, request.ActivityInstanceId);
        if (scope is not null) return OperationResult<RouletteSessionDto>.Fail(scope.Value.Message, scope.Value.Code);
        if (!RouletteRuntimeStates.IsPlayable(session!.Status)) return OperationResult<RouletteSessionDto>.Fail("اللعبة غير جارية حاليًا.", 409);
        if ((session.PendingActionStatus != "AutoResolved" && session.PendingActionStatus != "WaitingForPowerUp") || string.IsNullOrWhiteSpace(session.PendingTargetUserDiscordId))
            return OperationResult<RouletteSessionDto>.Ok(Map(session, userDiscordId));
        if (session.PendingActionStatus == "WaitingForPowerUp" && session.PendingActionExpiresAtUtc > DateTimeOffset.UtcNow)
            return OperationResult<RouletteSessionDto>.Fail("لا يزال بإمكان اللاعب استخدام خاصية.", 409);
        var target = session.Players.FirstOrDefault(x => x.DiscordUserId == session.PendingTargetUserDiscordId && x.IsAlive);
        if (target is null)
        {
            ClearPending(session);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return OperationResult<RouletteSessionDto>.Ok(Map(session, userDiscordId));
        }
        await EliminateAsync(session, target, session.CurrentTurnUserDiscordId ?? userDiscordId, ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        var dto = Map(session, userDiscordId);
        await PublishAsync(dto.GameSessionId, session.Status == RouletteRuntimeStates.Completed ? "RouletteRoundSettled" : "RouletteSessionUpdated", dto, ct);
        return OperationResult<RouletteSessionDto>.Ok(dto);
    }

    public async Task<OperationResult<RouletteSessionDto>> ReconnectAsync(Guid gameSessionId, RouletteScopeRequest request, TrustedDiscordUser user, CancellationToken ct = default)
    {
        var session = await LoadByGameSessionAsync(gameSessionId, ct);
        var scope = ValidateScope(session, request.GuildDiscordId, request.ChannelDiscordId, request.ActivityInstanceId);
        if (scope is not null) return OperationResult<RouletteSessionDto>.Fail(scope.Value.Message, scope.Value.Code);
        if (!session!.Players.Any(x => x.DiscordUserId == user.DiscordUserId)) return OperationResult<RouletteSessionDto>.Fail("أنت غير منضم لهذه الجولة.", 403);
        var activityPlayer = await db.ActivityPlayers.FirstOrDefaultAsync(x => x.ActivitySessionId == session.GameSession.ActivitySessionId && x.DiscordUserId == user.DiscordUserId, ct);
        if (activityPlayer is not null)
        {
            activityPlayer.ConnectionStatus = "Online";
            activityPlayer.LastSeenAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        return OperationResult<RouletteSessionDto>.Ok(Map(session, user.DiscordUserId));
    }

    public async Task<OperationResult<RouletteBetDto>> PlaceBetAsync(Guid gameSessionId, PlaceRouletteBetRequest request, string userDiscordId, CancellationToken ct = default)
    {
        var session = await LoadByGameSessionAsync(gameSessionId, ct);
        var scope = ValidateScope(session, request.GuildDiscordId, request.ChannelDiscordId, request.ActivityInstanceId);
        if (scope is not null) return OperationResult<RouletteBetDto>.Fail(scope.Value.Message, scope.Value.Code);
        if (session!.Status != RouletteRuntimeStates.BettingOpen) return OperationResult<RouletteBetDto>.Fail("لا يمكن تسجيل نية الرهان قبل بدء الجولة.", 409);
        if (!session.Players.Any(x => x.DiscordUserId == userDiscordId && x.IsAlive)) return OperationResult<RouletteBetDto>.Fail("أنت غير منضم لهذه الجولة.", 403);
        if (request.Amount < 0) return OperationResult<RouletteBetDto>.Fail("مبلغ الرهان غير صالح.", 400);
        if (request.Amount != decimal.Truncate(request.Amount)) return OperationResult<RouletteBetDto>.Fail("المحفظة الحالية تدعم عملات صحيحة فقط.", 400);
        var round = session.Rounds.OrderByDescending(x => x.RoundNumber).FirstOrDefault();
        if (round is null) return OperationResult<RouletteBetDto>.Fail("لا توجد جولة مفتوحة لتسجيل الرهان.", 409);
        var idempotency = Idempotency(request.IdempotencyKey, "bet", gameSessionId, userDiscordId, round.RoundNumber, request.BetType, request.BetValue);
        var existing = await db.RouletteBets.AsNoTracking().FirstOrDefaultAsync(x => x.RouletteRoundId == round.Id && x.DiscordUserId == userDiscordId && x.IdempotencyKey == idempotency, ct);
        if (existing is not null) return OperationResult<RouletteBetDto>.Ok(MapBet(existing));

        string? reservationId = null;
        if (request.Amount > 0)
        {
            var reservation = await platform.ReserveWalletAsync(new ReserveWalletRequest
            {
                DiscordGuildId = request.GuildDiscordId,
                DiscordUserId = userDiscordId,
                GameKey = GameKey,
                Amount = request.Amount,
                Currency = request.Currency,
                IdempotencyKey = idempotency
            }, ct);
            if (!reservation.Succeeded || string.IsNullOrWhiteSpace(reservation.ReservationId))
                return OperationResult<RouletteBetDto>.Fail(reservation.ErrorMessage ?? "تعذر حجز الرصيد.", 409);
            reservationId = reservation.ReservationId;
        }

        try
        {
            var bet = new RouletteBet
            {
                RouletteRoundId = round.Id,
                DiscordUserId = userDiscordId,
                BetType = Limit(request.BetType.Trim(), 64),
                BetValue = Limit(request.BetValue.Trim(), 120),
                Amount = request.Amount,
                Currency = Limit(request.Currency, 16),
                Status = request.Amount > 0 ? "PendingCommit" : "Accepted",
                IdempotencyKey = idempotency,
                WalletReservationId = reservationId
            };
            db.RouletteBets.Add(bet);
            await db.SaveChangesAsync(ct);
            if (!string.IsNullOrWhiteSpace(reservationId))
            {
                try
                {
                    logger.LogInformation("Committing Roulette wallet reservation {ReservationId} for bet {BetId}, gameSession {GameSessionId}, user {DiscordUserId}.", reservationId, bet.Id, gameSessionId, userDiscordId);
                    await platform.CommitWalletReservationAsync(reservationId, ct);
                    bet.Status = "Accepted";
                    await db.SaveChangesAsync(ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Roulette wallet reservation commit failed. reservation={ReservationId}, bet={BetId}, gameSession={GameSessionId}, guild={GuildDiscordId}, user={DiscordUserId}", reservationId, bet.Id, gameSessionId, request.GuildDiscordId, userDiscordId);
                    bet.Status = "CommitFailed";
                    await db.SaveChangesAsync(CancellationToken.None);
                    try { await platform.ReleaseWalletReservationAsync(reservationId, CancellationToken.None); }
                    catch (Exception releaseEx) { logger.LogWarning(releaseEx, "Could not release wallet reservation {ReservationId} after commit failure.", reservationId); }
                    return OperationResult<RouletteBetDto>.Fail("تعذر تأكيد عملية المحفظة. لم يتم قبول الرهان.", 409);
                }
            }
            return OperationResult<RouletteBetDto>.Ok(MapBet(bet));
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(reservationId))
            {
                try { await platform.ReleaseWalletReservationAsync(reservationId, CancellationToken.None); }
                catch (Exception ex) { logger.LogWarning(ex, "Could not release wallet reservation {ReservationId} after Roulette bet failure.", reservationId); }
            }
            throw;
        }
    }

    private async Task<OperationResult<GameAccessResult>> ValidateAccessAsync(string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct)
    {
        if (!ValidSnowflake(guildDiscordId) || !ValidSnowflake(channelDiscordId) || !ValidSnowflake(userDiscordId))
            return OperationResult<GameAccessResult>.Fail("بيانات Discord غير صالحة.");
        var result = await platform.ValidateGameAccessAsync(new ValidateGameAccessRequest { DiscordGuildId = guildDiscordId, DiscordChannelId = channelDiscordId, DiscordUserId = userDiscordId, GameKey = GameKey }, ct);
        if (!result.Allowed) return OperationResult<GameAccessResult>.Fail(result.DenialReason ?? "لعبة الروليت غير متاحة لهذا السيرفر.", 403);
        return OperationResult<GameAccessResult>.Ok(result);
    }

    private async Task<bool> HasActiveHostSessionAsync(string guildDiscordId, string channelDiscordId, string hostUserDiscordId, DateTimeOffset now, CancellationToken ct)
    {
        return await db.RouletteGameSessions.AsNoTracking()
            .Include(x => x.GameSession)
            .AnyAsync(x => x.GameSession.DiscordGuildId == guildDiscordId
                && x.GameSession.DiscordChannelId == channelDiscordId
                && x.HostUserDiscordId == hostUserDiscordId
                && ((x.Status == RouletteRuntimeStates.WaitingForPlayers && x.ExpiresAtUtc > now) || x.Status == RouletteRuntimeStates.BettingOpen), ct);
    }

    private async Task<RouletteGameSession?> LoadByGameSessionAsync(Guid gameSessionId, CancellationToken ct)
    {
        return await db.RouletteGameSessions
            .Include(x => x.GameSession).ThenInclude(x => x.Events)
            .Include(x => x.GameSession).ThenInclude(x => x.ActivitySession)
            .Include(x => x.Players)
            .Include(x => x.Rounds).ThenInclude(x => x.Bets)
            .Include(x => x.Rounds).ThenInclude(x => x.Payouts)
            .FirstOrDefaultAsync(x => x.GameSessionId == gameSessionId, ct);
    }

    private static (string Message, int Code)? ValidateScope(RouletteGameSession? session, string guildDiscordId, string channelDiscordId, string? activityInstanceId = null)
    {
        if (session is null) return ("غرفة الروليت غير موجودة.", 404);
        if (session.GameSession.DiscordGuildId != guildDiscordId || session.GameSession.DiscordChannelId != channelDiscordId) return ("لا تملك صلاحية الوصول لهذه الغرفة.", 403);
        var storedActivityInstanceId = session.GameSession.ActivitySession.DiscordActivityInstanceId;
        if (!string.IsNullOrWhiteSpace(storedActivityInstanceId)
            && !string.IsNullOrWhiteSpace(activityInstanceId)
            && !string.Equals(storedActivityInstanceId, activityInstanceId, StringComparison.Ordinal))
            return ("لا تملك صلاحية الوصول لهذه الجلسة من Activity مختلف.", 403);
        return null;
    }

    private async Task ExpireIfNeededAsync(RouletteGameSession session, CancellationToken ct)
    {
        if (session.Status == RouletteRuntimeStates.WaitingForPlayers && session.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            Transition(session, RouletteRuntimeStates.Expired);
            await db.SaveChangesAsync(ct);
        }
    }

    private static RoulettePlayer PickTarget(IReadOnlyList<RoulettePlayer> alive, string spinnerUserDiscordId)
    {
        var candidates = alive.Where(x => x.DiscordUserId != spinnerUserDiscordId).ToList();
        if (candidates.Count == 0) candidates = alive.ToList();
        return candidates[RandomNumberGenerator.GetInt32(candidates.Count)];
    }

    private async Task EliminateAsync(RouletteGameSession session, RoulettePlayer eliminated, string actorUserDiscordId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        ClearPending(session);
        eliminated.IsAlive = false;
        eliminated.EliminatedAtUtc = now;
        var actor = session.Players.FirstOrDefault(x => x.DiscordUserId == actorUserDiscordId);
        if (actor is not null && actor.Id != eliminated.Id) actor.Eliminations++;
        AddEvent(session.GameSession, session.CurrentRound, actorUserDiscordId, eliminated.DiscordUserId, "RoulettePlayerEliminated", Idempotency(null, "eliminate", session.GameSessionId, session.CurrentRound, eliminated.DiscordUserId), new { eliminated.Username, message = $"💥 تم إقصاء {eliminated.Username}." });

        var alive = session.Players.Where(x => x.IsAlive).OrderBy(x => x.Position).ToList();
        if (alive.Count > 1)
        {
            AdvanceTurn(session, actorUserDiscordId);
            return;
        }

        var winner = alive.SingleOrDefault();
        if (winner is null)
        {
            Transition(session, RouletteRuntimeStates.Cancelled);
            session.CompletedAtUtc = now;
            session.GameSession.CompletedAtUtc = now;
            session.CurrentTurnUserDiscordId = null;
            return;
        }

        Transition(session, RouletteRuntimeStates.Completed);
        session.CompletedAtUtc = now;
        session.GameSession.CompletedAtUtc = now;
        session.CurrentTurnUserDiscordId = null;
        session.GameSession.ResultJson = JsonSerializer.Serialize(new { winnerUserDiscordId = winner.DiscordUserId, winnerUsername = winner.Username, completedAt = now });
        db.GameResults.Add(new GameResult { GameSessionId = session.GameSessionId, GameKey = GameKey, DiscordUserId = winner.DiscordUserId, Score = 1, Won = true, PointsAwarded = 0, ResultJson = session.GameSession.ResultJson });
        var round = session.Rounds.OrderByDescending(x => x.RoundNumber).FirstOrDefault();
        if (round is not null && session.WinnerCoins > 0 && !round.Payouts.Any(x => x.DiscordUserId == winner.DiscordUserId))
        {
            round.Status = "PendingPayout";
            round.Payouts.Add(new RoulettePayout
            {
                DiscordUserId = winner.DiscordUserId,
                Amount = session.WinnerCoins,
                Currency = "coins",
                IdempotencyKey = Idempotency(null, "roulette-payout", session.GameSessionId, round.Id, winner.DiscordUserId),
                Status = "PendingPayout"
            });
        }
        AddEvent(session.GameSession, session.CurrentRound, actorUserDiscordId, winner.DiscordUserId, "RouletteGameCompleted", Idempotency(null, "complete", session.GameSessionId, winner.DiscordUserId), new { winner.Username, message = $"🏆 {winner.Username} فاز بلعبة الروليت!" });
        await Task.CompletedTask;
    }

    private static void AdvanceTurn(RouletteGameSession session, string fromUserDiscordId)
    {
        var ordered = session.Players.OrderBy(x => x.Position).ToList();
        var alive = ordered.Where(x => x.IsAlive).ToList();
        if (alive.Count <= 1) return;
        var startIndex = Math.Max(0, ordered.FindIndex(x => x.DiscordUserId == fromUserDiscordId));
        var next = ordered.Skip(startIndex + 1).Concat(ordered.Take(startIndex + 1)).First(x => x.IsAlive);
        session.CurrentTurnUserDiscordId = next.DiscordUserId;
        session.CurrentRound++;
    }

    private static void ClearPending(RouletteGameSession session)
    {
        session.PendingTargetUserDiscordId = null;
        session.PendingActionStatus = "None";
        session.PendingActionExpiresAtUtc = null;
    }

    private static void AddEvent(GameSession game, int round, string actor, string? target, string type, string idempotencyKey, object data)
    {
        game.Events.Add(new GameEvent
        {
            GameSession = game,
            GameKey = GameKey,
            EventType = type,
            Status = "Processed",
            PayloadJson = JsonSerializer.Serialize(new { round, actor, target, data }),
            IdempotencyKey = idempotencyKey,
            DiscordUserId = actor,
            ProcessedAtUtc = DateTimeOffset.UtcNow
        });
    }

    private async Task PublishAsync(Guid gameSessionId, string type, object payload, CancellationToken ct)
    {
        try { await realtime.PublishAsync(new RouletteRealtimeEvent { GameSessionId = gameSessionId, Type = type, Payload = payload }, ct); }
        catch (Exception ex) { logger.LogWarning(ex, "Could not publish Roulette realtime event {EventType} for session {GameSessionId}.", type, gameSessionId); }
    }

    private static RouletteSessionDto Map(RouletteGameSession x, string? currentUserDiscordId = null)
    {
        var players = x.Players.OrderBy(p => p.Position).Select(MapPlayer).ToList();
        var current = players.FirstOrDefault(p => p.UserDiscordId == x.CurrentTurnUserDiscordId);
        var pending = players.FirstOrDefault(p => p.UserDiscordId == x.PendingTargetUserDiscordId);
        return new RouletteSessionDto
        {
            Id = x.GameSessionId,
            GameSessionId = x.GameSessionId,
            ActivitySessionId = x.GameSession.ActivitySessionId,
            GuildDiscordId = x.GameSession.DiscordGuildId,
            ChannelDiscordId = x.GameSession.DiscordChannelId,
            HostUserDiscordId = x.HostUserDiscordId,
            HostUsername = x.HostUsername,
            Status = x.Status,
            MinPlayers = x.MinPlayers,
            MaxPlayers = x.MaxPlayers,
            WinnerCoins = x.WinnerCoins,
            SecondPlaceCoins = x.SecondPlaceCoins,
            ParticipationCoins = x.ParticipationCoins,
            CurrentRound = x.CurrentRound,
            ExpiresAt = x.ExpiresAtUtc,
            StartedAt = x.StartedAtUtc,
            CompletedAt = x.CompletedAtUtc,
            CanStart = x.Status == RouletteRuntimeStates.WaitingForPlayers && x.ExpiresAtUtc > DateTimeOffset.UtcNow && players.Count >= x.MinPlayers,
            IsCurrentUserJoined = !string.IsNullOrWhiteSpace(currentUserDiscordId) && players.Any(p => p.UserDiscordId == currentUserDiscordId),
            CurrentTurnUserDiscordId = x.CurrentTurnUserDiscordId,
            CurrentTurnUsername = current?.Username,
            CurrentTurnPlayer = current,
            PendingTargetUserDiscordId = x.PendingTargetUserDiscordId,
            PendingTargetUsername = pending?.Username,
            PendingTargetPlayer = pending,
            PendingActionStatus = x.PendingActionStatus,
            PendingActionExpiresAt = x.PendingActionExpiresAtUtc,
            LastSpinResult = Deserialize<RouletteSpinResultInfoDto>(x.LastSpinResultJson),
            Players = players,
            AlivePlayers = players.Where(p => p.IsAlive).ToList(),
            EliminatedPlayers = players.Where(p => !p.IsAlive).ToList(),
            Actions = x.GameSession.Events.OrderByDescending(a => a.CreatedAtUtc).Take(20).Select(MapAction).ToList(),
            Winner = x.Status == RouletteRuntimeStates.Completed ? players.FirstOrDefault(p => p.IsAlive) : null
        };
    }

    private static RoulettePlayerDto MapPlayer(RoulettePlayer x) => new()
    {
        UserDiscordId = x.DiscordUserId,
        Username = x.Username,
        DisplayName = x.DisplayName ?? x.Username,
        AvatarUrl = x.AvatarUrl,
        IsHost = x.IsHost,
        IsAlive = x.IsAlive,
        Position = x.Position,
        Eliminations = x.Eliminations,
        JoinedAt = x.JoinedAtUtc,
        EliminatedAt = x.EliminatedAtUtc
    };

    private static RouletteActionDto MapAction(GameEvent x)
    {
        var message = ""; var round = 0; string? target = null;
        try
        {
            using var doc = JsonDocument.Parse(x.PayloadJson);
            var root = doc.RootElement;
            if (root.TryGetProperty("round", out var roundNode)) round = roundNode.GetInt32();
            if (root.TryGetProperty("target", out var targetNode)) target = targetNode.GetString();
            if (root.TryGetProperty("data", out var data) && data.TryGetProperty("message", out var msg)) message = msg.GetString() ?? "";
        }
        catch { }
        if (string.IsNullOrWhiteSpace(message))
        {
            message = x.EventType switch
            {
                "RouletteSessionCreateRequested" => "تم إنشاء غرفة الروليت.",
                "RoulettePlayerJoined" => "انضم لاعب للغرفة.",
                "RoulettePlayerLeft" => "غادر لاعب الغرفة.",
                "RouletteRoundStarted" => "بدأت لعبة الروليت.",
                "RouletteRoundResult" => "تم تدوير العجلة.",
                "RoulettePlayerEliminated" => "تم إقصاء لاعب.",
                "RouletteGameCompleted" => "انتهت لعبة الروليت.",
                _ => x.EventType
            };
        }
        return new RouletteActionDto { RoundNumber = round, ActionType = x.EventType, ActorUserDiscordId = x.DiscordUserId ?? "", TargetUserDiscordId = target, Message = message, CreatedAt = x.CreatedAtUtc };
    }

    private static RouletteBetDto MapBet(RouletteBet x) => new() { Id = x.Id, RouletteRoundId = x.RouletteRoundId, DiscordUserId = x.DiscordUserId, BetType = x.BetType, BetValue = x.BetValue, Amount = x.Amount, Currency = x.Currency, Status = x.Status };
    private static void Transition(RouletteGameSession session, string next)
    {
        if (!RouletteRuntimeStates.CanTransition(session.Status, next))
            throw new InvalidOperationException($"Invalid Roulette state transition from {session.Status} to {next}.");
        session.Status = next;
        session.GameSession.Status = next;
    }
    private static T? Deserialize<T>(string? json) { if (string.IsNullOrWhiteSpace(json)) return default; try { return JsonSerializer.Deserialize<T>(json); } catch { return default; } }
    private static string Idempotency(string? value, params object[] parts) => !string.IsNullOrWhiteSpace(value) ? Limit(value.Trim(), 160) : Limit(string.Join(":", parts.Select(x => x?.ToString() ?? "")) + ":" + Guid.NewGuid().ToString("N"), 160);
    private static bool ValidSnowflake(string value) => ulong.TryParse(value, out _);
    private static string CleanUsername(string value, string fallback) => Limit(string.IsNullOrWhiteSpace(value) ? fallback : value.Trim(), 80);
    private static string Limit(string value, int max) => value[..Math.Min(value.Length, max)];
    private static string? LimitNullable(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : Limit(value.Trim(), max);
}
