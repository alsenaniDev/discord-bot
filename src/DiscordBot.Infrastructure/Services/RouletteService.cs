using System.Data;
using System.Text.Json;
using DiscordBot.Domain.Constants;
using DiscordBot.Domain.Entities;
using DiscordBot.Domain.Enums;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Data.Configurations;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Infrastructure.Services;

public interface IRouletteService
{
    Task<RouletteSettingsDto?> GetSettingsAsync(Guid guildId, CancellationToken ct = default);
    Task<GameHubResult<RouletteSettingsDto>> UpdateSettingsAsync(Guid guildId, UpdateRouletteSettingsRequest request, CancellationToken ct = default);
    Task<GameHubResult<GameWalletDto>> GetWalletAsync(string guildDiscordId, string userDiscordId, CancellationToken ct = default);
    Task<GameHubResult<RouletteRoomDto>> CreateRoomAsync(CreateRouletteRoomRequest request, string userDiscordId, string username, CancellationToken ct = default);
    Task<GameHubResult<IReadOnlyList<RouletteRoomDto>>> GetOpenRoomsAsync(string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default);
    Task<GameHubResult<RouletteRoomDto>> GetRoomAsync(Guid roomId, string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default);
    Task<GameHubResult<RouletteRoomDto>> JoinRoomAsync(Guid roomId, string guildDiscordId, string channelDiscordId, string userDiscordId, string username, CancellationToken ct = default);
    Task<GameHubResult<RouletteRoomDto>> LeaveRoomAsync(Guid roomId, string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default);
    Task<GameHubResult<RouletteRoomDto>> StartRoomAsync(Guid roomId, string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default);
    Task<GameHubResult<RouletteSpinResultDto>> SpinAsync(Guid roomId, string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default);
    Task<GameHubResult<PrepareRouletteJoinResponse>> PrepareJoinAsync(Guid roomId, PrepareRouletteJoinRequest request, CancellationToken ct = default);
    Task<GameHubResult<PendingRouletteIntentDto?>> ConsumePendingIntentAsync(string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default);
    Task<IReadOnlyList<PendingRoulettePublishActionDto>> GetPendingPublishActionsAsync(CancellationToken ct = default);
    Task<bool> AckPublishActionAsync(Guid actionId, AckRoulettePublishActionRequest request, CancellationToken ct = default);
}

public class RouletteService(AppDbContext db, ILogger<RouletteService> logger) : IRouletteService
{
    public async Task<RouletteSettingsDto?> GetSettingsAsync(Guid guildId, CancellationToken ct = default)
    {
        if (!await db.Guilds.AsNoTracking().AnyAsync(x => x.Id == guildId && x.IsActive, ct)) return null;
        return MapSettings(guildId, await db.RouletteGuildSettings.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == guildId, ct));
    }

    public async Task<GameHubResult<RouletteSettingsDto>> UpdateSettingsAsync(Guid guildId, UpdateRouletteSettingsRequest request, CancellationToken ct = default)
    {
        var error = ValidateSettings(request); if (error is not null) return GameHubResult<RouletteSettingsDto>.Fail(error);
        if (!await db.Guilds.AnyAsync(x => x.Id == guildId && x.IsActive, ct)) return GameHubResult<RouletteSettingsDto>.Fail("السيرفر غير موجود أو غير مربوط.", 404);
        var value = await db.RouletteGuildSettings.FirstOrDefaultAsync(x => x.GuildId == guildId, ct);
        if (value is null) { value = new RouletteGuildSettings { GuildId = guildId }; db.RouletteGuildSettings.Add(value); }
        value.MinPlayers = request.MinPlayers; value.MaxPlayers = request.MaxPlayers; value.WinnerCoins = request.WinnerCoins;
        value.SecondPlaceCoins = request.SecondPlaceCoins; value.ParticipationCoins = request.ParticipationCoins;
        value.JoinWindowSeconds = request.JoinWindowSeconds; value.TurnSeconds = request.TurnSeconds;
        value.AnnounceRoomCreated = request.AnnounceRoomCreated; value.AnnounceWinner = request.AnnounceWinner;
        await db.SaveChangesAsync(ct);
        return GameHubResult<RouletteSettingsDto>.Ok(MapSettings(guildId, value));
    }

    public async Task<GameHubResult<GameWalletDto>> GetWalletAsync(string guildDiscordId, string userDiscordId, CancellationToken ct = default)
    {
        if (!ValidSnowflake(guildDiscordId) || !ValidSnowflake(userDiscordId)) return GameHubResult<GameWalletDto>.Fail("بيانات Discord غير صالحة.");
        var guildId = await db.Guilds.AsNoTracking().Where(x => x.DiscordGuildId == guildDiscordId && x.IsActive).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
        if (!guildId.HasValue) return GameHubResult<GameWalletDto>.Fail("هذا السيرفر غير مربوط بمنصة البوت.", 404);
        var balance = await db.GameWallets.AsNoTracking().Where(x => x.GuildId == guildId && x.UserDiscordId == userDiscordId).Select(x => (int?)x.Balance).FirstOrDefaultAsync(ct) ?? 0;
        return GameHubResult<GameWalletDto>.Ok(new GameWalletDto { Balance = balance });
    }

    public async Task<GameHubResult<RouletteRoomDto>> CreateRoomAsync(CreateRouletteRoomRequest request, string userDiscordId, string username, CancellationToken ct = default)
    {
        var context = await EnabledContextAsync(request.GuildDiscordId, request.ChannelDiscordId, ct);
        if (!context.Succeeded) return GameHubResult<RouletteRoomDto>.Fail(context.Error!, context.StatusCode);
        if (!ValidSnowflake(userDiscordId)) return GameHubResult<RouletteRoomDto>.Fail("تعذر التحقق من حساب Discord.");
        var (guild, game, general, gameSetting) = context.Value!;
        var now = DateTimeOffset.UtcNow;
        if (await db.RouletteRooms.AnyAsync(x => x.GuildId == guild.Id && x.HostUserDiscordId == userDiscordId && (x.Status == "Waiting" || x.Status == "InProgress") && x.ExpiresAt > now, ct))
            return GameHubResult<RouletteRoomDto>.Fail("لديك غرفة روليت نشطة بالفعل.", 409);
        var last = await db.RouletteRooms.Where(x => x.GuildId == guild.Id && x.HostUserDiscordId == userDiscordId).MaxAsync(x => (DateTimeOffset?)x.CreatedAt, ct);
        if (last.HasValue && gameSetting.CooldownSeconds > 0 && last.Value.AddSeconds(gameSetting.CooldownSeconds) > now)
            return GameHubResult<RouletteRoomDto>.Fail($"انتظر {(int)Math.Ceiling((last.Value.AddSeconds(gameSetting.CooldownSeconds) - now).TotalSeconds)} ثانية قبل إنشاء غرفة جديدة.", 429);
        var today = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        if (gameSetting.MaxPlaysPerDay > 0 && await db.RouletteRooms.CountAsync(x => x.GuildId == guild.Id && x.HostUserDiscordId == userDiscordId && x.CreatedAt >= today, ct) >= gameSetting.MaxPlaysPerDay)
            return GameHubResult<RouletteRoomDto>.Fail("وصلت للحد اليومي للعبة الروليت. ارجع لنا بكرة!", 429);

        var settings = await db.RouletteGuildSettings.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == guild.Id, ct) ?? new RouletteGuildSettings { GuildId = guild.Id };
        var cleanName = CleanUsername(username, userDiscordId);
        var room = new RouletteRoom
        {
            GuildId = guild.Id, PlatformGameDefinitionId = game.Id, ChannelDiscordId = request.ChannelDiscordId,
            HostUserDiscordId = userDiscordId, HostUsername = cleanName, MinPlayers = settings.MinPlayers, MaxPlayers = settings.MaxPlayers,
            WinnerCoins = settings.WinnerCoins, SecondPlaceCoins = settings.SecondPlaceCoins, ParticipationCoins = settings.ParticipationCoins,
            ExpiresAt = now.AddSeconds(settings.JoinWindowSeconds)
        };
        room.Players.Add(new RouletteRoomPlayer { UserDiscordId = userDiscordId, Username = cleanName, IsHost = true, Position = 1, JoinedAt = now });
        room.Actions.Add(Action(room.Id, 0, userDiscordId, null, "RoomCreated"));
        if (settings.AnnounceRoomCreated && !string.IsNullOrWhiteSpace(general.GamesChannelDiscordId))
            room.PublishActions.Add(Publish(room, general.GamesChannelDiscordId, "RoomInvite", new PublishPayload { HostUsername = cleanName, MinPlayers = room.MinPlayers, MaxPlayers = room.MaxPlayers, PlayersCount = 1, JoinWindowSeconds = settings.JoinWindowSeconds }));
        db.RouletteRooms.Add(room); await db.SaveChangesAsync(ct);
        logger.LogInformation("Roulette room {RoomId} created in guild {GuildId}, channel {ChannelId}, by {UserId}.", room.Id, guild.DiscordGuildId, room.ChannelDiscordId, userDiscordId);
        return GameHubResult<RouletteRoomDto>.Ok(MapRoom(room, guild.DiscordGuildId));
    }

    public async Task<GameHubResult<IReadOnlyList<RouletteRoomDto>>> GetOpenRoomsAsync(string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default)
    {
        var context = await EnabledContextAsync(guildDiscordId, channelDiscordId, ct);
        if (!context.Succeeded) return GameHubResult<IReadOnlyList<RouletteRoomDto>>.Fail(context.Error!, context.StatusCode);
        var guild = context.Value!.Guild; var now = DateTimeOffset.UtcNow;
        var rooms = await db.RouletteRooms.AsNoTracking().Include(x => x.Players)
            .Where(x => x.GuildId == guild.Id && x.ChannelDiscordId == channelDiscordId && x.Status == "Waiting" && x.ExpiresAt > now && x.Players.Count < x.MaxPlayers)
            .OrderByDescending(x => x.CreatedAt).Take(30).ToListAsync(ct);
        return GameHubResult<IReadOnlyList<RouletteRoomDto>>.Ok(rooms.Select(x => MapRoom(x, guildDiscordId)).ToList());
    }

    public async Task<GameHubResult<RouletteRoomDto>> GetRoomAsync(Guid roomId, string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default)
    {
        var room = await LoadRoomAsync(roomId, ct); var error = ValidateRoomScope(room, guildDiscordId, channelDiscordId);
        if (error is not null) return GameHubResult<RouletteRoomDto>.Fail(error.Value.Message, error.Value.Code);
        await ExpireIfNeededAsync(room!, ct);
        return GameHubResult<RouletteRoomDto>.Ok(MapRoom(room!, guildDiscordId));
    }

    public async Task<GameHubResult<RouletteRoomDto>> JoinRoomAsync(Guid roomId, string guildDiscordId, string channelDiscordId, string userDiscordId, string username, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var room = await LoadRoomAsync(roomId, ct); var error = ValidateRoomScope(room, guildDiscordId, channelDiscordId);
        if (error is not null) return GameHubResult<RouletteRoomDto>.Fail(error.Value.Message, error.Value.Code);
        if (room!.Status != "Waiting") return GameHubResult<RouletteRoomDto>.Fail("هذه الجولة لم تعد متاحة للانضمام.", 409);
        if (room.ExpiresAt <= DateTimeOffset.UtcNow) { room.Status = "Expired"; await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return GameHubResult<RouletteRoomDto>.Fail("انتهت مدة الانضمام لهذه الجولة.", 410); }
        if (room.Players.Any(x => x.UserDiscordId == userDiscordId)) return GameHubResult<RouletteRoomDto>.Fail("أنت منضم لهذه الجولة بالفعل.", 409);
        if (room.Players.Count >= room.MaxPlayers) return GameHubResult<RouletteRoomDto>.Fail("اكتمل عدد اللاعبين في هذه الجولة.", 409);
        var player = new RouletteRoomPlayer { RouletteRoomId = room.Id, UserDiscordId = userDiscordId, Username = CleanUsername(username, userDiscordId), Position = room.Players.Count + 1 };
        room.Players.Add(player); room.Actions.Add(Action(room.Id, 0, userDiscordId, null, "PlayerJoined"));
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return GameHubResult<RouletteRoomDto>.Ok(MapRoom(room, guildDiscordId));
    }

    public async Task<GameHubResult<RouletteRoomDto>> LeaveRoomAsync(Guid roomId, string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var room = await LoadRoomAsync(roomId, ct); var error = ValidateRoomScope(room, guildDiscordId, channelDiscordId);
        if (error is not null) return GameHubResult<RouletteRoomDto>.Fail(error.Value.Message, error.Value.Code);
        if (room!.Status != "Waiting") return GameHubResult<RouletteRoomDto>.Fail("لا يمكن مغادرة الجولة بعد بدء اللعبة.", 409);
        var player = room.Players.FirstOrDefault(x => x.UserDiscordId == userDiscordId);
        if (player is null) return GameHubResult<RouletteRoomDto>.Fail("أنت غير منضم لهذه الجولة.", 404);
        room.Actions.Add(Action(room.Id, 0, userDiscordId, null, "PlayerLeft")); db.RouletteRoomPlayers.Remove(player); room.Players.Remove(player);
        if (player.IsHost)
        {
            var next = room.Players.OrderBy(x => x.JoinedAt).FirstOrDefault();
            if (next is null) room.Status = "Cancelled";
            else { next.IsHost = true; room.HostUserDiscordId = next.UserDiscordId; room.HostUsername = next.Username; }
        }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return GameHubResult<RouletteRoomDto>.Ok(MapRoom(room, guildDiscordId));
    }

    public async Task<GameHubResult<RouletteRoomDto>> StartRoomAsync(Guid roomId, string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var room = await LoadRoomAsync(roomId, ct); var error = ValidateRoomScope(room, guildDiscordId, channelDiscordId);
        if (error is not null) return GameHubResult<RouletteRoomDto>.Fail(error.Value.Message, error.Value.Code);
        if (room!.HostUserDiscordId != userDiscordId) return GameHubResult<RouletteRoomDto>.Fail("فقط صاحب الغرفة يقدر يبدأ اللعبة.", 403);
        if (room.Status != "Waiting") return GameHubResult<RouletteRoomDto>.Fail("هذه الجولة بدأت أو انتهت مسبقًا.", 409);
        if (room.ExpiresAt <= DateTimeOffset.UtcNow) { room.Status = "Expired"; await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return GameHubResult<RouletteRoomDto>.Fail("انتهت مدة الانضمام لهذه الجولة.", 410); }
        if (room.Players.Count < room.MinPlayers) return GameHubResult<RouletteRoomDto>.Fail($"تحتاج {room.MinPlayers} لاعبين على الأقل لبدء اللعبة.", 409);
        room.Status = "InProgress"; room.StartedAt = DateTimeOffset.UtcNow; room.CurrentRound = 1;
        room.Actions.Add(Action(room.Id, 1, userDiscordId, null, "GameStarted"));
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return GameHubResult<RouletteRoomDto>.Ok(MapRoom(room, guildDiscordId));
    }

    public async Task<GameHubResult<RouletteSpinResultDto>> SpinAsync(Guid roomId, string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var room = await LoadRoomAsync(roomId, ct); var error = ValidateRoomScope(room, guildDiscordId, channelDiscordId);
        if (error is not null) return GameHubResult<RouletteSpinResultDto>.Fail(error.Value.Message, error.Value.Code);
        if (room!.HostUserDiscordId != userDiscordId) return GameHubResult<RouletteSpinResultDto>.Fail("فقط صاحب الغرفة يقدر يدوّر الروليت.", 403);
        if (room.Status != "InProgress") return GameHubResult<RouletteSpinResultDto>.Fail("اللعبة غير جاهزة للتدوير.", 409);
        var alive = room.Players.Where(x => x.IsAlive).ToList();
        if (alive.Count <= 1) return GameHubResult<RouletteSpinResultDto>.Fail("انتهت هذه الجولة مسبقًا.", 409);
        var eliminated = alive[Random.Shared.Next(alive.Count)]; var now = DateTimeOffset.UtcNow;
        room.Actions.Add(Action(room.Id, room.CurrentRound, userDiscordId, null, "Spin"));
        eliminated.IsAlive = false; eliminated.EliminatedAt = now;
        room.Actions.Add(Action(room.Id, room.CurrentRound, userDiscordId, eliminated.UserDiscordId, "PlayerEliminated", new { eliminated.Username }));
        var remaining = alive.Count - 1;
        if (remaining > 1) room.CurrentRound++;
        else
        {
            var winner = room.Players.Single(x => x.IsAlive); room.Status = "Completed"; room.CompletedAt = now;
            room.Actions.Add(Action(room.Id, room.CurrentRound, userDiscordId, winner.UserDiscordId, "GameCompleted", new { winner.Username }));
            await AwardCompletionAsync(room, winner, eliminated, now, ct);
            var settings = await db.RouletteGuildSettings.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == room.GuildId, ct);
            var gameSetting = await db.GuildGameSettings.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == room.GuildId && x.PlatformGameDefinitionId == room.PlatformGameDefinitionId, ct);
            if (settings?.AnnounceWinner != false && gameSetting?.PublishResultAfterGame == true)
                room.PublishActions.Add(Publish(room, room.ChannelDiscordId, "Winner", new PublishPayload { WinnerUsername = winner.Username, WinnerCoins = room.WinnerCoins, PlayersCount = room.Players.Count, CurrentRound = room.CurrentRound }));
        }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        logger.LogInformation("Roulette room {RoomId} round {Round} eliminated user {UserId}; status {Status}.", room.Id, room.CurrentRound, eliminated.UserDiscordId, room.Status);
        return GameHubResult<RouletteSpinResultDto>.Ok(new RouletteSpinResultDto { Room = MapRoom(room, guildDiscordId), EliminatedPlayer = MapPlayer(eliminated) });
    }

    public async Task<GameHubResult<PrepareRouletteJoinResponse>> PrepareJoinAsync(Guid roomId, PrepareRouletteJoinRequest request, CancellationToken ct = default)
    {
        if (!ValidSnowflake(request.UserDiscordId)) return GameHubResult<PrepareRouletteJoinResponse>.Fail("تعذر التحقق من حساب Discord.");
        var context = await EnabledContextAsync(request.GuildDiscordId, request.ChannelDiscordId, ct);
        if (!context.Succeeded) return GameHubResult<PrepareRouletteJoinResponse>.Fail(context.Error!, context.StatusCode);
        var room = await LoadRoomAsync(roomId, ct); var error = ValidateRoomScope(room, request.GuildDiscordId, request.ChannelDiscordId);
        if (error is not null) return GameHubResult<PrepareRouletteJoinResponse>.Fail(error.Value.Message, error.Value.Code);
        if (room!.Status != "Waiting") return GameHubResult<PrepareRouletteJoinResponse>.Fail("هذه الجولة لم تعد متاحة للانضمام.", 409);
        if (room.ExpiresAt <= DateTimeOffset.UtcNow) return GameHubResult<PrepareRouletteJoinResponse>.Fail("انتهت مدة الانضمام لهذه الجولة.", 410);
        if (room.Players.Any(x => x.UserDiscordId == request.UserDiscordId)) return GameHubResult<PrepareRouletteJoinResponse>.Fail("أنت منضم لهذه الجولة بالفعل.", 409);
        if (room.Players.Count >= room.MaxPlayers) return GameHubResult<PrepareRouletteJoinResponse>.Fail("اكتمل عدد اللاعبين في هذه الجولة.", 409);
        var now = DateTimeOffset.UtcNow;
        var stale = await db.RouletteJoinIntents.Where(x => x.GuildId == room.GuildId && x.UserDiscordId == request.UserDiscordId && x.Status == "Pending").ToListAsync(ct);
        foreach (var item in stale) item.Status = "Expired";
        var intent = new RouletteJoinIntent { GuildId = room.GuildId, RouletteRoomId = room.Id, UserDiscordId = request.UserDiscordId, ChannelDiscordId = request.ChannelDiscordId, ExpiresAt = now.AddMinutes(2) };
        db.RouletteJoinIntents.Add(intent); await db.SaveChangesAsync(ct);
        return GameHubResult<PrepareRouletteJoinResponse>.Ok(new PrepareRouletteJoinResponse { JoinIntentId = intent.Id, ExpiresAt = intent.ExpiresAt });
    }

    public async Task<GameHubResult<PendingRouletteIntentDto?>> ConsumePendingIntentAsync(string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default)
    {
        var guildId = await db.Guilds.AsNoTracking().Where(x => x.DiscordGuildId == guildDiscordId && x.IsActive).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
        if (!guildId.HasValue) return GameHubResult<PendingRouletteIntentDto?>.Fail("هذا السيرفر غير مربوط بمنصة البوت.", 404);
        var now = DateTimeOffset.UtcNow;
        var intent = await db.RouletteJoinIntents.Include(x => x.RouletteRoom).Where(x => x.GuildId == guildId && x.ChannelDiscordId == channelDiscordId && x.UserDiscordId == userDiscordId && x.Status == "Pending").OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        if (intent is null) return GameHubResult<PendingRouletteIntentDto?>.Ok(null);
        if (intent.ExpiresAt <= now || intent.RouletteRoom.Status != "Waiting" || intent.RouletteRoom.ExpiresAt <= now) { intent.Status = "Expired"; await db.SaveChangesAsync(ct); return GameHubResult<PendingRouletteIntentDto?>.Ok(null); }
        intent.Status = "Consumed"; intent.ConsumedAt = now; await db.SaveChangesAsync(ct);
        return GameHubResult<PendingRouletteIntentDto?>.Ok(new PendingRouletteIntentDto { RoomId = intent.RouletteRoomId });
    }

    public async Task<IReadOnlyList<PendingRoulettePublishActionDto>> GetPendingPublishActionsAsync(CancellationToken ct = default)
    {
        var actions = await db.RoulettePublishActions.AsNoTracking().Include(x => x.Guild).Include(x => x.RouletteRoom)
            .Where(x => x.Status == "Pending").OrderBy(x => x.CreatedAt).Take(100).ToListAsync(ct);
        return actions.Select(x =>
        {
            var p = JsonSerializer.Deserialize<PublishPayload>(x.PayloadJson) ?? new PublishPayload();
            return new PendingRoulettePublishActionDto { Id = x.Id, RoomId = x.RouletteRoomId, DiscordGuildId = x.Guild.DiscordGuildId, ChannelDiscordId = x.ChannelDiscordId, Type = x.Type, HostUsername = p.HostUsername, WinnerUsername = p.WinnerUsername, MinPlayers = p.MinPlayers, MaxPlayers = p.MaxPlayers, PlayersCount = p.PlayersCount, JoinWindowSeconds = p.JoinWindowSeconds, WinnerCoins = p.WinnerCoins, CurrentRound = p.CurrentRound };
        }).ToList();
    }

    public async Task<bool> AckPublishActionAsync(Guid actionId, AckRoulettePublishActionRequest request, CancellationToken ct = default)
    {
        var action = await db.RoulettePublishActions.Include(x => x.RouletteRoom).FirstOrDefaultAsync(x => x.Id == actionId && x.Status == "Pending", ct); if (action is null) return false;
        action.Status = request.Success ? "Processed" : "Failed"; action.ProcessedAt = DateTimeOffset.UtcNow; action.ErrorMessage = request.Success ? null : Limit(request.ErrorMessage ?? "تعذر نشر رسالة الروليت.", 2000);
        if (request.Success && action.Type == "RoomInvite" && ValidSnowflake(request.MessageDiscordId ?? string.Empty)) action.RouletteRoom.InviteMessageDiscordId = request.MessageDiscordId;
        await db.SaveChangesAsync(ct); return true;
    }

    private async Task AwardCompletionAsync(RouletteRoom room, RouletteRoomPlayer winner, RouletteRoomPlayer second, DateTimeOffset now, CancellationToken ct)
    {
        foreach (var player in room.Players)
        {
            var type = player.Id == winner.Id ? "RouletteWinnerReward" : player.Id == second.Id ? "RouletteSecondPlaceReward" : "RouletteParticipationReward";
            var amount = player.Id == winner.Id ? room.WinnerCoins : player.Id == second.Id ? room.SecondPlaceCoins : room.ParticipationCoins;
            if (amount > 0 && !await db.GameWalletTransactions.AnyAsync(x => x.ReferenceId == room.Id && x.UserDiscordId == player.UserDiscordId && x.Type == type, ct))
            {
                var wallet = await db.GameWallets.FirstOrDefaultAsync(x => x.GuildId == room.GuildId && x.UserDiscordId == player.UserDiscordId, ct);
                if (wallet is null) { wallet = new GameWallet { GuildId = room.GuildId, UserDiscordId = player.UserDiscordId }; db.GameWallets.Add(wallet); }
                wallet.Balance += amount;
                db.GameWalletTransactions.Add(new GameWalletTransaction { GuildId = room.GuildId, UserDiscordId = player.UserDiscordId, Amount = amount, Type = type, Reason = "مكافأة لعبة الروليت", ReferenceId = room.Id });
            }
            var stats = await db.GamePlayers.FirstOrDefaultAsync(x => x.GuildId == room.GuildId && x.UserDiscordId == player.UserDiscordId, ct);
            if (stats is null) { stats = new GamePlayer { GuildId = room.GuildId, UserDiscordId = player.UserDiscordId }; db.GamePlayers.Add(stats); }
            stats.Username = player.Username; stats.GamesPlayed++; stats.LastPlayedAt = now;
            if (player.Id == winner.Id) { stats.Wins++; stats.CurrentStreak++; stats.BestStreak = Math.Max(stats.BestStreak, stats.CurrentStreak); }
            else { stats.Losses++; stats.CurrentStreak = 0; }
        }
    }

    private async Task<GameHubResult<EnabledContext>> EnabledContextAsync(string guildDiscordId, string channelDiscordId, CancellationToken ct)
    {
        if (!ValidSnowflake(guildDiscordId) || !ValidSnowflake(channelDiscordId)) return GameHubResult<EnabledContext>.Fail("بيانات السيرفر أو الروم غير صالحة.");
        var guild = await db.Guilds.AsNoTracking().FirstOrDefaultAsync(x => x.DiscordGuildId == guildDiscordId && x.IsActive, ct);
        if (guild is null) return GameHubResult<EnabledContext>.Fail("هذا السيرفر غير مربوط بمنصة البوت.", 404);
        var general = await db.GuildGamesSettings.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == guild.Id, ct);
        if (general?.IsEnabled != true) return GameHubResult<EnabledContext>.Fail("الألعاب غير مفعّلة في هذا السيرفر.", 403);
        if (general.GamesChannelDiscordId != channelDiscordId) return GameHubResult<EnabledContext>.Fail("🎮 الألعاب متاحة فقط في روم الألعاب المحدد.", 403);
        var game = await db.PlatformGameDefinitions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == PlatformGameDefinitionConfiguration.RouletteId && x.IsEnabledGlobally, ct);
        if (game is null) return GameHubResult<EnabledContext>.Fail("لعبة الروليت غير متاحة حاليًا.", 404);
        if (!IsPlanAllowed(await GuildPlanAsync(guild.Id, ct), game.RequiredPlan)) return GameHubResult<EnabledContext>.Fail("لعبة الروليت متاحة في باقة Pro وما فوق.", 403);
        var setting = await db.GuildGameSettings.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == guild.Id && x.PlatformGameDefinitionId == game.Id && x.IsEnabledForGuild, ct);
        if (setting is null) return GameHubResult<EnabledContext>.Fail("لعبة الروليت غير مفعّلة في هذا السيرفر.", 403);
        return GameHubResult<EnabledContext>.Ok(new EnabledContext(guild, game, general, setting));
    }

    private async Task<RouletteRoom?> LoadRoomAsync(Guid id, CancellationToken ct) => await db.RouletteRooms.Include(x => x.Guild).Include(x => x.Players).Include(x => x.Actions).FirstOrDefaultAsync(x => x.Id == id, ct);
    private static (string Message, int Code)? ValidateRoomScope(RouletteRoom? room, string guildDiscordId, string channelDiscordId)
    {
        if (room is null) return ("غرفة الروليت غير موجودة.", 404);
        if (room.Guild.DiscordGuildId != guildDiscordId || room.ChannelDiscordId != channelDiscordId) return ("لا تملك صلاحية الوصول لهذه الغرفة.", 403);
        return null;
    }
    private async Task ExpireIfNeededAsync(RouletteRoom room, CancellationToken ct) { if (room.Status == "Waiting" && room.ExpiresAt <= DateTimeOffset.UtcNow) { room.Status = "Expired"; await db.SaveChangesAsync(ct); } }
    private async Task<string> GuildPlanAsync(Guid guildId, CancellationToken ct) { var x = await db.GuildSubscriptions.AsNoTracking().Include(x => x.SubscriptionPlan).FirstOrDefaultAsync(x => x.GuildId == guildId, ct); return x is not null && x.Status == GuildSubscriptionStatus.Active && (!x.ExpiresAt.HasValue || x.ExpiresAt > DateTimeOffset.UtcNow) ? x.SubscriptionPlan.Key : PlanKeys.Free; }
    private static bool IsPlanAllowed(string actual, string required) { var rank = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { [PlanKeys.Free] = 0, [PlanKeys.Basic] = 1, [PlanKeys.Pro] = 2, [PlanKeys.Premium] = 3 }; return rank.GetValueOrDefault(actual, -1) >= rank.GetValueOrDefault(required, int.MaxValue); }
    private static RouletteRoundAction Action(Guid roomId, int round, string actor, string? target, string type, object? data = null) => new() { RouletteRoomId = roomId, RoundNumber = round, ActorUserDiscordId = actor, TargetUserDiscordId = target, ActionType = type, DataJson = JsonSerializer.Serialize(data ?? new { }) };
    private static RoulettePublishAction Publish(RouletteRoom room, string channelId, string type, PublishPayload payload) => new() { GuildId = room.GuildId, RouletteRoomId = room.Id, ChannelDiscordId = channelId, Type = type, PayloadJson = JsonSerializer.Serialize(payload) };
    private static RouletteRoomDto MapRoom(RouletteRoom x, string guildDiscordId) { var players = x.Players.OrderBy(p => p.Position).Select(MapPlayer).ToList(); return new RouletteRoomDto { Id = x.Id, GuildDiscordId = guildDiscordId, ChannelDiscordId = x.ChannelDiscordId, HostUserDiscordId = x.HostUserDiscordId, HostUsername = x.HostUsername, Status = x.Status, MinPlayers = x.MinPlayers, MaxPlayers = x.MaxPlayers, WinnerCoins = x.WinnerCoins, SecondPlaceCoins = x.SecondPlaceCoins, ParticipationCoins = x.ParticipationCoins, CurrentRound = x.CurrentRound, ExpiresAt = x.ExpiresAt, StartedAt = x.StartedAt, CompletedAt = x.CompletedAt, CanStart = x.Status == "Waiting" && x.ExpiresAt > DateTimeOffset.UtcNow && players.Count >= x.MinPlayers, Players = players, Winner = x.Status == "Completed" ? players.FirstOrDefault(p => p.IsAlive) : null }; }
    private static RoulettePlayerDto MapPlayer(RouletteRoomPlayer x) => new() { UserDiscordId = x.UserDiscordId, Username = x.Username, IsHost = x.IsHost, IsAlive = x.IsAlive, Position = x.Position, Eliminations = x.Eliminations, JoinedAt = x.JoinedAt, EliminatedAt = x.EliminatedAt };
    private static RouletteSettingsDto MapSettings(Guid guildId, RouletteGuildSettings? x) => new() { GuildId = guildId, MinPlayers = x?.MinPlayers ?? 2, MaxPlayers = x?.MaxPlayers ?? 6, WinnerCoins = x?.WinnerCoins ?? 100, SecondPlaceCoins = x?.SecondPlaceCoins ?? 50, ParticipationCoins = x?.ParticipationCoins ?? 10, JoinWindowSeconds = x?.JoinWindowSeconds ?? 120, TurnSeconds = x?.TurnSeconds ?? 30, AnnounceRoomCreated = x?.AnnounceRoomCreated ?? true, AnnounceWinner = x?.AnnounceWinner ?? true };
    private static string? ValidateSettings(RouletteSettingsDto x) { if (x.MinPlayers is < 2 or > 10) return "الحد الأدنى للاعبين يجب أن يكون بين 2 و10."; if (x.MaxPlayers < x.MinPlayers || x.MaxPlayers > 10) return "الحد الأعلى يجب أن يكون بين الحد الأدنى و10."; if (x.WinnerCoins is < 0 or > 1000) return "مكافأة الفائز يجب أن تكون بين 0 و1000."; if (x.SecondPlaceCoins is < 0 or > 500) return "مكافأة المركز الثاني يجب أن تكون بين 0 و500."; if (x.ParticipationCoins is < 0 or > 100) return "مكافأة المشاركة يجب أن تكون بين 0 و100."; if (x.JoinWindowSeconds is < 30 or > 300) return "مدة انتظار الانضمام يجب أن تكون بين 30 و300 ثانية."; if (x.TurnSeconds is < 10 or > 120) return "مدة الدور يجب أن تكون بين 10 و120 ثانية."; return null; }
    private static bool ValidSnowflake(string value) => ulong.TryParse(value, out _);
    private static string CleanUsername(string value, string fallback) => Limit(string.IsNullOrWhiteSpace(value) ? fallback : value.Trim(), 80);
    private static string Limit(string value, int max) => value[..Math.Min(value.Length, max)];
    private sealed record EnabledContext(Guild Guild, PlatformGameDefinition Game, GuildGamesSettings General, GuildGameSetting GameSetting);
    private sealed class PublishPayload { public string HostUsername { get; set; } = string.Empty; public string WinnerUsername { get; set; } = string.Empty; public int MinPlayers { get; set; } public int MaxPlayers { get; set; } public int PlayersCount { get; set; } public int JoinWindowSeconds { get; set; } public int WinnerCoins { get; set; } public int CurrentRound { get; set; } }
}
