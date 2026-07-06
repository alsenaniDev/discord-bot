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
    Task<GameHubResult<PowerUpStoreDto>> GetStoreAsync(string guildDiscordId, string userDiscordId, CancellationToken ct = default);
    Task<GameHubResult<PurchasePowerUpResponse>> PurchasePowerUpAsync(PurchasePowerUpRequest request, string userDiscordId, CancellationToken ct = default);
    Task<GameHubResult<RouletteRoomDto>> CreateRoomAsync(CreateRouletteRoomRequest request, string userDiscordId, string username, CancellationToken ct = default);
    Task<GameHubResult<IReadOnlyList<RouletteRoomDto>>> GetOpenRoomsAsync(string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default);
    Task<GameHubResult<RouletteRoomDto>> GetRoomAsync(Guid roomId, string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default);
    Task<GameHubResult<RouletteRoomDto>> JoinRoomAsync(Guid roomId, string guildDiscordId, string channelDiscordId, string userDiscordId, string username, CancellationToken ct = default);
    Task<GameHubResult<RouletteRoomDto>> LeaveRoomAsync(Guid roomId, string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default);
    Task<GameHubResult<RouletteRoomDto>> StartRoomAsync(Guid roomId, string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default);
    Task<GameHubResult<RouletteSpinResultDto>> SpinAsync(Guid roomId, string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default);
    Task<GameHubResult<RouletteRoomDto>> UsePowerUpAsync(Guid roomId, UsePowerUpRequest request, string userDiscordId, CancellationToken ct = default);
    Task<GameHubResult<RouletteRoomDto>> ResolvePendingActionAsync(Guid roomId, CreateRouletteRoomRequest request, string userDiscordId, CancellationToken ct = default);
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
        await EnsurePowerUpSettingsAsync(guildId, ct);
        var settings = MapSettings(guildId, await db.RouletteGuildSettings.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == guildId, ct));
        settings.PowerUps = await PowerUpSettingsAsync(guildId, ct);
        return settings;
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
        await EnsurePowerUpSettingsAsync(guildId, ct);
        foreach (var incoming in request.PowerUps)
        {
            var setting = await db.GuildPowerUpSettings.Include(x => x.GamePowerUpDefinition).FirstOrDefaultAsync(x => x.GuildId == guildId && x.GamePowerUpDefinition.Key == incoming.Key, ct);
            if (setting is null) continue;
            setting.IsEnabledForGuild = incoming.IsEnabledForGuild;
            setting.Price = Math.Clamp(incoming.Price, 0, 10000);
            setting.MaxUsesPerGame = Math.Clamp(incoming.MaxUsesPerGame, 1, 10);
        }
        await db.SaveChangesAsync(ct);
        var dto = MapSettings(guildId, value);
        dto.PowerUps = await PowerUpSettingsAsync(guildId, ct);
        return GameHubResult<RouletteSettingsDto>.Ok(dto);
    }

    public async Task<GameHubResult<GameWalletDto>> GetWalletAsync(string guildDiscordId, string userDiscordId, CancellationToken ct = default)
    {
        if (!ValidSnowflake(guildDiscordId) || !ValidSnowflake(userDiscordId)) return GameHubResult<GameWalletDto>.Fail("بيانات Discord غير صالحة.");
        var guildId = await db.Guilds.AsNoTracking().Where(x => x.DiscordGuildId == guildDiscordId && x.IsActive).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
        if (!guildId.HasValue) return GameHubResult<GameWalletDto>.Fail("هذا السيرفر غير مربوط بمنصة البوت.", 404);
        var balance = await db.GameWallets.AsNoTracking().Where(x => x.GuildId == guildId && x.UserDiscordId == userDiscordId).Select(x => (int?)x.Balance).FirstOrDefaultAsync(ct) ?? 0;
        return GameHubResult<GameWalletDto>.Ok(new GameWalletDto { Balance = balance });
    }

    public async Task<GameHubResult<PowerUpStoreDto>> GetStoreAsync(string guildDiscordId, string userDiscordId, CancellationToken ct = default)
    {
        if (!ValidSnowflake(guildDiscordId) || !ValidSnowflake(userDiscordId)) return GameHubResult<PowerUpStoreDto>.Fail("بيانات Discord غير صالحة.");
        var guildId = await db.Guilds.AsNoTracking().Where(x => x.DiscordGuildId == guildDiscordId && x.IsActive).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
        if (!guildId.HasValue) return GameHubResult<PowerUpStoreDto>.Fail("هذا السيرفر غير مربوط بمنصة البوت.", 404);
        if (!await db.GuildGamesSettings.AsNoTracking().AnyAsync(x => x.GuildId == guildId && x.IsEnabled, ct)) return GameHubResult<PowerUpStoreDto>.Fail("الألعاب غير مفعّلة في هذا السيرفر.", 403);
        await EnsurePowerUpSettingsAsync(guildId.Value, ct);
        var balance = await db.GameWallets.AsNoTracking().Where(x => x.GuildId == guildId && x.UserDiscordId == userDiscordId).Select(x => (int?)x.Balance).FirstOrDefaultAsync(ct) ?? 0;
        var owned = await db.PlayerPowerUpInventories.AsNoTracking().Where(x => x.GuildId == guildId && x.UserDiscordId == userDiscordId).ToDictionaryAsync(x => x.GamePowerUpDefinitionId, x => x.Quantity, ct);
        var items = await db.GuildPowerUpSettings.AsNoTracking().Include(x => x.GamePowerUpDefinition)
            .Where(x => x.GuildId == guildId && x.GamePowerUpDefinition.IsEnabledGlobally)
            .OrderBy(x => x.GamePowerUpDefinition.Key).ToListAsync(ct);
        return GameHubResult<PowerUpStoreDto>.Ok(new PowerUpStoreDto
        {
            Balance = balance,
            Items = items.Select(x => new PowerUpStoreItemDto
            {
                Key = x.GamePowerUpDefinition.Key,
                Name = x.GamePowerUpDefinition.Name,
                Description = x.GamePowerUpDefinition.Description,
                Icon = x.GamePowerUpDefinition.Icon,
                IsEnabledForGuild = x.IsEnabledForGuild,
                Price = x.Price,
                MaxUsesPerGame = x.MaxUsesPerGame,
                OwnedQuantity = owned.GetValueOrDefault(x.GamePowerUpDefinitionId)
            }).ToList()
        });
    }

    public async Task<GameHubResult<PurchasePowerUpResponse>> PurchasePowerUpAsync(PurchasePowerUpRequest request, string userDiscordId, CancellationToken ct = default)
    {
        if (!ValidSnowflake(request.GuildDiscordId) || !ValidSnowflake(userDiscordId)) return GameHubResult<PurchasePowerUpResponse>.Fail("بيانات Discord غير صالحة.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var guildId = await db.Guilds.Where(x => x.DiscordGuildId == request.GuildDiscordId && x.IsActive).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
        if (!guildId.HasValue) return GameHubResult<PurchasePowerUpResponse>.Fail("هذا السيرفر غير مربوط بمنصة البوت.", 404);
        if (!await db.GuildGamesSettings.AnyAsync(x => x.GuildId == guildId && x.IsEnabled, ct)) return GameHubResult<PurchasePowerUpResponse>.Fail("الألعاب غير مفعّلة في هذا السيرفر.", 403);
        await EnsurePowerUpSettingsAsync(guildId.Value, ct);
        var setting = await db.GuildPowerUpSettings.Include(x => x.GamePowerUpDefinition)
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.GamePowerUpDefinition.Key == request.PowerUpKey && x.GamePowerUpDefinition.IsEnabledGlobally, ct);
        if (setting is null || !setting.IsEnabledForGuild) return GameHubResult<PurchasePowerUpResponse>.Fail("هذه الخاصية غير متاحة في هذا السيرفر.", 404);
        var wallet = await db.GameWallets.FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserDiscordId == userDiscordId, ct);
        if (wallet is null) { wallet = new GameWallet { GuildId = guildId.Value, UserDiscordId = userDiscordId }; db.GameWallets.Add(wallet); await db.SaveChangesAsync(ct); }
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"GameWallets\" SET \"UpdatedAt\" = \"UpdatedAt\" WHERE \"Id\" = {wallet.Id}", ct);
        wallet = await db.GameWallets.FirstAsync(x => x.Id == wallet.Id, ct);
        if (wallet.Balance < setting.Price) return GameHubResult<PurchasePowerUpResponse>.Fail("رصيدك غير كافٍ لشراء هذه الخاصية.", 409);
        wallet.Balance -= setting.Price;
        var inventory = await db.PlayerPowerUpInventories.FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserDiscordId == userDiscordId && x.GamePowerUpDefinitionId == setting.GamePowerUpDefinitionId, ct);
        if (inventory is null) { inventory = new PlayerPowerUpInventory { GuildId = guildId.Value, UserDiscordId = userDiscordId, GamePowerUpDefinitionId = setting.GamePowerUpDefinitionId }; db.PlayerPowerUpInventories.Add(inventory); }
        inventory.Quantity++;
        db.GameWalletTransactions.Add(new GameWalletTransaction { GuildId = guildId.Value, UserDiscordId = userDiscordId, Amount = -setting.Price, Type = "PurchasePowerUp", Reason = $"شراء خاصية {setting.GamePowerUpDefinition.Name}" });
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return GameHubResult<PurchasePowerUpResponse>.Ok(new PurchasePowerUpResponse { Balance = wallet.Balance, PowerUpKey = setting.GamePowerUpDefinition.Key, OwnedQuantity = inventory.Quantity });
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
        // Lock only this room while validating capacity. Serializable transactions can
        // abort otherwise-valid mobile joins when two Activity clients arrive together.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        // A no-op UPDATE is an explicit PostgreSQL row lock and is valid through
        // ExecuteNonQuery on every supported Npgsql version.
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"RouletteRooms\" SET \"UpdatedAt\" = \"UpdatedAt\" WHERE \"Id\" = {roomId}", ct);
        var room = await LoadRoomAsync(roomId, ct); var error = ValidateRoomScope(room, guildDiscordId, channelDiscordId);
        if (error is not null) return GameHubResult<RouletteRoomDto>.Fail(error.Value.Message, error.Value.Code);
        if (room!.Status != "Waiting") return GameHubResult<RouletteRoomDto>.Fail("هذه الجولة لم تعد متاحة للانضمام.", 409);
        if (room.ExpiresAt <= DateTimeOffset.UtcNow) { room.Status = "Expired"; await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return GameHubResult<RouletteRoomDto>.Fail("انتهت مدة الانضمام لهذه الجولة.", 410); }
        if (room.Players.Any(x => x.UserDiscordId == userDiscordId)) return GameHubResult<RouletteRoomDto>.Fail("أنت منضم لهذه الجولة بالفعل.", 409);
        if (room.Players.Count >= room.MaxPlayers) return GameHubResult<RouletteRoomDto>.Fail("اكتمل عدد اللاعبين في هذه الجولة.", 409);
        var player = new RouletteRoomPlayer { RouletteRoomId = room.Id, RouletteRoom = room, UserDiscordId = userDiscordId, Username = CleanUsername(username, userDiscordId), Position = room.Players.Count + 1 };
        db.RouletteRoomPlayers.Add(player);
        if (!room.Players.Contains(player)) room.Players.Add(player);
        AddAction(room, 0, userDiscordId, null, "PlayerJoined");
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        logger.LogInformation("User {UserId} joined Roulette room {RoomId}. Players: {PlayerCount}/{MaxPlayers}.", userDiscordId, room.Id, room.Players.Count, room.MaxPlayers);
        return GameHubResult<RouletteRoomDto>.Ok(MapRoom(room, guildDiscordId));
    }

    public async Task<GameHubResult<RouletteRoomDto>> LeaveRoomAsync(Guid roomId, string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"RouletteRooms\" SET \"UpdatedAt\" = \"UpdatedAt\" WHERE \"Id\" = {roomId}", ct);
        var room = await LoadRoomAsync(roomId, ct); var error = ValidateRoomScope(room, guildDiscordId, channelDiscordId);
        if (error is not null) return GameHubResult<RouletteRoomDto>.Fail(error.Value.Message, error.Value.Code);
        if (room!.Status != "Waiting") return GameHubResult<RouletteRoomDto>.Fail("لا يمكن مغادرة الجولة بعد بدء اللعبة.", 409);
        var player = room.Players.FirstOrDefault(x => x.UserDiscordId == userDiscordId);
        if (player is null) return GameHubResult<RouletteRoomDto>.Fail("أنت غير منضم لهذه الجولة.", 404);
        AddAction(room, 0, userDiscordId, null, "PlayerLeft");
        // ExecuteDelete avoids EF relationship/orphan tracking errors when the
        // tracked player is also removed from the room DTO collection.
        await db.RouletteRoomPlayers.Where(x => x.Id == player.Id).ExecuteDeleteAsync(ct);
        db.Entry(player).State = EntityState.Detached;
        room.Players.Remove(player);
        if (player.IsHost)
        {
            var next = room.Players.OrderBy(x => x.JoinedAt).FirstOrDefault();
            if (next is null)
            {
                room.Status = "Cancelled";
                var pendingActions = await db.RoulettePublishActions.Where(x => x.RouletteRoomId == room.Id && x.Status == "Pending").ToListAsync(ct);
                foreach (var action in pendingActions) { action.Status = "Cancelled"; action.ProcessedAt = DateTimeOffset.UtcNow; }
            }
            else { next.IsHost = true; room.HostUserDiscordId = next.UserDiscordId; room.HostUsername = next.Username; }
        }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        logger.LogInformation("User {UserId} left Roulette room {RoomId}. New status: {Status}; new host: {HostUserId}.", userDiscordId, room.Id, room.Status, room.HostUserDiscordId);
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
        room.CurrentTurnUserDiscordId = room.Players.OrderBy(x => x.Position).First(x => x.IsAlive).UserDiscordId;
        room.PendingActionStatus = "None";
        AddAction(room, 1, userDiscordId, null, "GameStarted");
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return GameHubResult<RouletteRoomDto>.Ok(MapRoom(room, guildDiscordId));
    }

    public async Task<GameHubResult<RouletteSpinResultDto>> SpinAsync(Guid roomId, string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var room = await LoadRoomAsync(roomId, ct); var error = ValidateRoomScope(room, guildDiscordId, channelDiscordId);
        if (error is not null) return GameHubResult<RouletteSpinResultDto>.Fail(error.Value.Message, error.Value.Code);
        if (room!.Status != "InProgress") return GameHubResult<RouletteSpinResultDto>.Fail("اللعبة غير جاهزة للتدوير.", 409);
        if (room.PendingActionStatus == "WaitingForPowerUp") return GameHubResult<RouletteSpinResultDto>.Fail("يوجد إجراء معلق، انتظر حتى ينتهي وقت اللاعب أو يتم استخدام خاصية.", 409);
        if (room.CurrentTurnUserDiscordId != userDiscordId) return GameHubResult<RouletteSpinResultDto>.Fail("ليس دورك الآن.", 403);
        var alive = room.Players.Where(x => x.IsAlive).ToList();
        if (alive.Count <= 1) return GameHubResult<RouletteSpinResultDto>.Fail("انتهت هذه الجولة مسبقًا.", 409);
        var spinner = alive.First(x => x.UserDiscordId == userDiscordId);
        var target = PickTarget(room, userDiscordId);
        var settings = await db.RouletteGuildSettings.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == room.GuildId, ct);
        SetPending(room, spinner, target, DateTimeOffset.UtcNow, settings?.TurnSeconds ?? 30);
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        logger.LogInformation("Roulette room {RoomId} round {Round} pending target {TargetUserId} by spinner {SpinnerUserId}.", room.Id, room.CurrentRound, target.UserDiscordId, userDiscordId);
        return GameHubResult<RouletteSpinResultDto>.Ok(new RouletteSpinResultDto { Room = MapRoom(room, guildDiscordId), TargetPlayer = MapPlayer(target) });
    }

    public async Task<GameHubResult<RouletteRoomDto>> UsePowerUpAsync(Guid roomId, UsePowerUpRequest request, string userDiscordId, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"RouletteRooms\" SET \"UpdatedAt\" = \"UpdatedAt\" WHERE \"Id\" = {roomId}", ct);
        var room = await LoadRoomAsync(roomId, ct); var error = ValidateRoomScope(room, request.GuildDiscordId, request.ChannelDiscordId);
        if (error is not null) return GameHubResult<RouletteRoomDto>.Fail(error.Value.Message, error.Value.Code);
        if (room!.Status != "InProgress") return GameHubResult<RouletteRoomDto>.Fail("اللعبة غير جارية حاليًا.", 409);
        if (room.PendingActionStatus != "WaitingForPowerUp" || string.IsNullOrWhiteSpace(room.PendingTargetUserDiscordId)) return GameHubResult<RouletteRoomDto>.Fail("لا يوجد إجراء معلق لاستخدام خاصية.", 409);
        if (room.PendingTargetUserDiscordId != userDiscordId) return GameHubResult<RouletteRoomDto>.Fail("هذه الخاصية متاحة للاعب المستهدف فقط.", 403);
        var target = room.Players.FirstOrDefault(x => x.UserDiscordId == userDiscordId && x.IsAlive);
        var spinner = room.Players.FirstOrDefault(x => x.UserDiscordId == room.CurrentTurnUserDiscordId && x.IsAlive);
        if (target is null || spinner is null) return GameHubResult<RouletteRoomDto>.Fail("تعذر تحديد اللاعبين في هذه الجولة.", 409);
        await EnsurePowerUpSettingsAsync(room.GuildId, ct);
        var setting = await db.GuildPowerUpSettings.Include(x => x.GamePowerUpDefinition)
            .FirstOrDefaultAsync(x => x.GuildId == room.GuildId && x.GamePowerUpDefinition.Key == request.PowerUpKey && x.GamePowerUpDefinition.IsEnabledGlobally, ct);
        if (setting is null || !setting.IsEnabledForGuild) return GameHubResult<RouletteRoomDto>.Fail("هذه الخاصية غير متاحة.", 404);
        var uses = await db.RoulettePowerUpUsages.CountAsync(x => x.RouletteRoomId == room.Id && x.UserDiscordId == userDiscordId && x.GamePowerUpDefinitionId == setting.GamePowerUpDefinitionId, ct);
        if (uses >= setting.MaxUsesPerGame) return GameHubResult<RouletteRoomDto>.Fail("وصلت للحد الأقصى لاستخدام هذه الخاصية في الجولة.", 409);
        var inventory = await db.PlayerPowerUpInventories.FirstOrDefaultAsync(x => x.GuildId == room.GuildId && x.UserDiscordId == userDiscordId && x.GamePowerUpDefinitionId == setting.GamePowerUpDefinitionId, ct);
        if (inventory is null || inventory.Quantity <= 0) return GameHubResult<RouletteRoomDto>.Fail("لا تملك هذه الخاصية في مخزونك.", 409);
        inventory.Quantity--;
        db.RoulettePowerUpUsages.Add(new RoulettePowerUpUsage { RouletteRoomId = room.Id, UserDiscordId = userDiscordId, GamePowerUpDefinitionId = setting.GamePowerUpDefinitionId, RoundNumber = room.CurrentRound, ResultJson = JsonSerializer.Serialize(new { key = setting.GamePowerUpDefinition.Key, usedAt = DateTimeOffset.UtcNow }) });
        switch (setting.GamePowerUpDefinition.Key)
        {
            case "shield":
                ClearPending(room);
                AddAction(room, room.CurrentRound, userDiscordId, target.UserDiscordId, "PowerUpShield", new { target.Username, message = $"🛡️ {target.Username} استخدم الدرع ونجا من الإقصاء!" });
                AdvanceTurn(room, spinner.UserDiscordId);
                break;
            case "reverse":
                ClearPending(room);
                AddAction(room, room.CurrentRound, userDiscordId, spinner.UserDiscordId, "PowerUpReverse", new { targetUsername = target.Username, spinnerUsername = spinner.Username, message = $"🔁 {target.Username} استخدم عكس الهجمة! تم عكس الإقصاء على {spinner.Username}." });
                await EliminateAsync(room, spinner, target.UserDiscordId, ct);
                break;
            case "respin":
                ClearPending(room);
                AddAction(room, room.CurrentRound, userDiscordId, target.UserDiscordId, "PowerUpReSpin", new { target.Username, message = $"🎡 {target.Username} استخدم إعادة اللف! العجلة تدور من جديد." });
                var newTarget = PickTarget(room, spinner.UserDiscordId, target.UserDiscordId);
                var settings = await db.RouletteGuildSettings.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == room.GuildId, ct);
                SetPending(room, spinner, newTarget, DateTimeOffset.UtcNow, settings?.TurnSeconds ?? 30);
                break;
            default:
                return GameHubResult<RouletteRoomDto>.Fail("هذه الخاصية غير مدعومة حاليًا.", 400);
        }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return GameHubResult<RouletteRoomDto>.Ok(MapRoom(room, request.GuildDiscordId));
    }

    public async Task<GameHubResult<RouletteRoomDto>> ResolvePendingActionAsync(Guid roomId, CreateRouletteRoomRequest request, string userDiscordId, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"RouletteRooms\" SET \"UpdatedAt\" = \"UpdatedAt\" WHERE \"Id\" = {roomId}", ct);
        var room = await LoadRoomAsync(roomId, ct); var error = ValidateRoomScope(room, request.GuildDiscordId, request.ChannelDiscordId);
        if (error is not null) return GameHubResult<RouletteRoomDto>.Fail(error.Value.Message, error.Value.Code);
        if (room!.Status != "InProgress") return GameHubResult<RouletteRoomDto>.Fail("اللعبة غير جارية حاليًا.", 409);
        if (room.PendingActionStatus != "WaitingForPowerUp" || string.IsNullOrWhiteSpace(room.PendingTargetUserDiscordId)) return GameHubResult<RouletteRoomDto>.Ok(MapRoom(room, request.GuildDiscordId));
        if (room.PendingActionExpiresAt > DateTimeOffset.UtcNow) return GameHubResult<RouletteRoomDto>.Fail("لا يزال بإمكان اللاعب استخدام خاصية.", 409);
        var target = room.Players.FirstOrDefault(x => x.UserDiscordId == room.PendingTargetUserDiscordId && x.IsAlive);
        if (target is null) { ClearPending(room); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return GameHubResult<RouletteRoomDto>.Ok(MapRoom(room, request.GuildDiscordId)); }
        await EliminateAsync(room, target, room.CurrentTurnUserDiscordId ?? userDiscordId, ct);
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return GameHubResult<RouletteRoomDto>.Ok(MapRoom(room, request.GuildDiscordId));
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

    private async Task EnsurePowerUpSettingsAsync(Guid guildId, CancellationToken ct)
    {
        var definitions = await db.GamePowerUpDefinitions.Where(x => x.IsEnabledGlobally).ToListAsync(ct);
        var existing = await db.GuildPowerUpSettings.Where(x => x.GuildId == guildId).Select(x => x.GamePowerUpDefinitionId).ToListAsync(ct);
        foreach (var definition in definitions.Where(x => !existing.Contains(x.Id)))
            db.GuildPowerUpSettings.Add(new GuildPowerUpSetting { GuildId = guildId, GamePowerUpDefinitionId = definition.Id, IsEnabledForGuild = true, Price = definition.DefaultPrice, MaxUsesPerGame = 1 });
        if (db.ChangeTracker.HasChanges()) await db.SaveChangesAsync(ct);
    }

    private async Task<List<RoulettePowerUpSettingDto>> PowerUpSettingsAsync(Guid guildId, CancellationToken ct)
    {
        return await db.GuildPowerUpSettings.AsNoTracking().Include(x => x.GamePowerUpDefinition)
            .Where(x => x.GuildId == guildId && x.GamePowerUpDefinition.IsEnabledGlobally)
            .OrderBy(x => x.GamePowerUpDefinition.Key)
            .Select(x => new RoulettePowerUpSettingDto
            {
                Key = x.GamePowerUpDefinition.Key,
                Name = x.GamePowerUpDefinition.Name,
                Description = x.GamePowerUpDefinition.Description,
                Icon = x.GamePowerUpDefinition.Icon,
                IsEnabledForGuild = x.IsEnabledForGuild,
                Price = x.Price,
                MaxUsesPerGame = x.MaxUsesPerGame
            }).ToListAsync(ct);
    }

    private static RouletteRoomPlayer PickTarget(RouletteRoom room, string spinnerUserDiscordId, string? excludeUserDiscordId = null)
    {
        var candidates = room.Players.Where(x => x.IsAlive && x.UserDiscordId != excludeUserDiscordId).ToList();
        var nonSpinner = candidates.Where(x => x.UserDiscordId != spinnerUserDiscordId).ToList();
        if (nonSpinner.Count > 0) candidates = nonSpinner;
        return candidates[Random.Shared.Next(candidates.Count)];
    }

    private void SetPending(RouletteRoom room, RouletteRoomPlayer spinner, RouletteRoomPlayer target, DateTimeOffset now, int turnSeconds)
    {
        room.PendingTargetUserDiscordId = target.UserDiscordId;
        room.PendingActionStatus = "WaitingForPowerUp";
        room.PendingActionExpiresAt = now.AddSeconds(Math.Clamp(turnSeconds, 10, 120));
        room.LastSpinResultJson = JsonSerializer.Serialize(new RouletteSpinResultInfoDto
        {
            SpinnerUserDiscordId = spinner.UserDiscordId,
            SpinnerUsername = spinner.Username,
            TargetUserDiscordId = target.UserDiscordId,
            TargetUsername = target.Username,
            ResultType = "PendingElimination",
            CreatedAt = now
        });
        AddAction(room, room.CurrentRound, spinner.UserDiscordId, target.UserDiscordId, "Spin", new { spinnerUsername = spinner.Username, targetUsername = target.Username, message = $"🎡 العجلة اختارت {target.Username}. لديه فرصة لاستخدام خاصية قبل الإقصاء." });
    }

    private static void ClearPending(RouletteRoom room)
    {
        room.PendingTargetUserDiscordId = null;
        room.PendingActionStatus = "None";
        room.PendingActionExpiresAt = null;
    }

    private void AdvanceTurn(RouletteRoom room, string fromUserDiscordId)
    {
        var ordered = room.Players.OrderBy(x => x.Position).ToList();
        var alive = ordered.Where(x => x.IsAlive).ToList();
        if (alive.Count <= 1) return;
        var startIndex = Math.Max(0, ordered.FindIndex(x => x.UserDiscordId == fromUserDiscordId));
        var next = ordered.Skip(startIndex + 1).Concat(ordered.Take(startIndex + 1)).First(x => x.IsAlive);
        room.CurrentTurnUserDiscordId = next.UserDiscordId;
        room.CurrentRound++;
    }

    private async Task EliminateAsync(RouletteRoom room, RouletteRoomPlayer eliminated, string actorUserDiscordId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        ClearPending(room);
        eliminated.IsAlive = false; eliminated.EliminatedAt = now;
        var actor = room.Players.FirstOrDefault(x => x.UserDiscordId == actorUserDiscordId);
        if (actor is not null && actor.Id != eliminated.Id) actor.Eliminations++;
        AddAction(room, room.CurrentRound, actorUserDiscordId, eliminated.UserDiscordId, "PlayerEliminated", new { eliminated.Username, message = $"💥 تم إقصاء {eliminated.Username}." });
        var alive = room.Players.Where(x => x.IsAlive).OrderBy(x => x.Position).ToList();
        if (alive.Count > 1)
        {
            AdvanceTurn(room, actorUserDiscordId);
            return;
        }
        var winner = alive.SingleOrDefault();
        if (winner is null) { room.Status = "Cancelled"; room.CompletedAt = now; room.CurrentTurnUserDiscordId = null; return; }
        room.Status = "Completed"; room.CompletedAt = now; room.CurrentTurnUserDiscordId = null;
        AddAction(room, room.CurrentRound, actorUserDiscordId, winner.UserDiscordId, "GameCompleted", new { winner.Username, message = $"🏆 {winner.Username} فاز بلعبة الروليت!" });
        await AwardCompletionAsync(room, winner, eliminated, now, ct);
        var settings = await db.RouletteGuildSettings.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == room.GuildId, ct);
        var gameSetting = await db.GuildGameSettings.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == room.GuildId && x.PlatformGameDefinitionId == room.PlatformGameDefinitionId, ct);
        if (settings?.AnnounceWinner != false && gameSetting?.PublishResultAfterGame == true)
            AddPublishAction(room, room.ChannelDiscordId, "Winner", new PublishPayload { WinnerUsername = winner.Username, WinnerCoins = room.WinnerCoins, PlayersCount = room.Players.Count, CurrentRound = room.CurrentRound });
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
    private void AddAction(RouletteRoom room, int round, string actor, string? target, string type, object? data = null)
    {
        var action = Action(room.Id, round, actor, target, type, data); action.RouletteRoom = room;
        db.RouletteRoundActions.Add(action);
        if (!room.Actions.Contains(action)) room.Actions.Add(action);
    }
    private void AddPublishAction(RouletteRoom room, string channelId, string type, PublishPayload payload)
    {
        var action = Publish(room, channelId, type, payload); action.RouletteRoom = room; action.Guild = room.Guild;
        db.RoulettePublishActions.Add(action);
        if (!room.PublishActions.Contains(action)) room.PublishActions.Add(action);
    }
    private static RouletteRoomDto MapRoom(RouletteRoom x, string guildDiscordId)
    {
        var players = x.Players.OrderBy(p => p.Position).Select(MapPlayer).ToList();
        var current = players.FirstOrDefault(p => p.UserDiscordId == x.CurrentTurnUserDiscordId);
        var pending = players.FirstOrDefault(p => p.UserDiscordId == x.PendingTargetUserDiscordId);
        return new RouletteRoomDto
        {
            Id = x.Id,
            GuildDiscordId = guildDiscordId,
            ChannelDiscordId = x.ChannelDiscordId,
            HostUserDiscordId = x.HostUserDiscordId,
            HostUsername = x.HostUsername,
            Status = x.Status,
            MinPlayers = x.MinPlayers,
            MaxPlayers = x.MaxPlayers,
            WinnerCoins = x.WinnerCoins,
            SecondPlaceCoins = x.SecondPlaceCoins,
            ParticipationCoins = x.ParticipationCoins,
            CurrentRound = x.CurrentRound,
            ExpiresAt = x.ExpiresAt,
            StartedAt = x.StartedAt,
            CompletedAt = x.CompletedAt,
            CanStart = x.Status == "Waiting" && x.ExpiresAt > DateTimeOffset.UtcNow && players.Count >= x.MinPlayers,
            CurrentTurnUserDiscordId = x.CurrentTurnUserDiscordId,
            CurrentTurnUsername = current?.Username,
            PendingTargetUserDiscordId = x.PendingTargetUserDiscordId,
            PendingTargetUsername = pending?.Username,
            PendingActionStatus = string.IsNullOrWhiteSpace(x.PendingActionStatus) ? "None" : x.PendingActionStatus,
            PendingActionExpiresAt = x.PendingActionExpiresAt,
            LastSpinResult = DeserializeSpinResult(x.LastSpinResultJson),
            Players = players,
            Actions = x.Actions.OrderByDescending(a => a.CreatedAt).Take(20).Select(MapAction).ToList(),
            Winner = x.Status == "Completed" ? players.FirstOrDefault(p => p.IsAlive) : null
        };
    }
    private static RoulettePlayerDto MapPlayer(RouletteRoomPlayer x) => new() { UserDiscordId = x.UserDiscordId, Username = x.Username, IsHost = x.IsHost, IsAlive = x.IsAlive, Position = x.Position, Eliminations = x.Eliminations, JoinedAt = x.JoinedAt, EliminatedAt = x.EliminatedAt };
    private static RouletteSpinResultInfoDto? DeserializeSpinResult(string? json) { if (string.IsNullOrWhiteSpace(json)) return null; try { return JsonSerializer.Deserialize<RouletteSpinResultInfoDto>(json); } catch { return null; } }
    private static RouletteActionDto MapAction(RouletteRoundAction x)
    {
        var message = "";
        try { message = JsonDocument.Parse(x.DataJson).RootElement.TryGetProperty("message", out var node) ? node.GetString() ?? "" : ""; } catch { }
        if (string.IsNullOrWhiteSpace(message))
        {
            message = x.ActionType switch
            {
                "RoomCreated" => "تم إنشاء غرفة الروليت.",
                "PlayerJoined" => "انضم لاعب للغرفة.",
                "PlayerLeft" => "غادر لاعب الغرفة.",
                "GameStarted" => "بدأت لعبة الروليت.",
                "Spin" => "تم تدوير العجلة.",
                "PlayerEliminated" => "تم إقصاء لاعب.",
                "PowerUpShield" => "تم استخدام الدرع.",
                "PowerUpReverse" => "تم استخدام عكس الهجمة.",
                "PowerUpReSpin" => "تم استخدام إعادة اللف.",
                "GameCompleted" => "انتهت لعبة الروليت.",
                _ => x.ActionType
            };
        }
        return new RouletteActionDto { RoundNumber = x.RoundNumber, ActionType = x.ActionType, ActorUserDiscordId = x.ActorUserDiscordId, TargetUserDiscordId = x.TargetUserDiscordId, Message = message, CreatedAt = x.CreatedAt };
    }
    private static RouletteSettingsDto MapSettings(Guid guildId, RouletteGuildSettings? x) => new() { GuildId = guildId, MinPlayers = x?.MinPlayers ?? 2, MaxPlayers = x?.MaxPlayers ?? 6, WinnerCoins = x?.WinnerCoins ?? 100, SecondPlaceCoins = x?.SecondPlaceCoins ?? 50, ParticipationCoins = x?.ParticipationCoins ?? 10, JoinWindowSeconds = x?.JoinWindowSeconds ?? 120, TurnSeconds = x?.TurnSeconds ?? 30, AnnounceRoomCreated = x?.AnnounceRoomCreated ?? true, AnnounceWinner = x?.AnnounceWinner ?? true };
    private static string? ValidateSettings(RouletteSettingsDto x) { if (x.MinPlayers is < 2 or > 10) return "الحد الأدنى للاعبين يجب أن يكون بين 2 و10."; if (x.MaxPlayers < x.MinPlayers || x.MaxPlayers > 10) return "الحد الأعلى يجب أن يكون بين الحد الأدنى و10."; if (x.WinnerCoins is < 0 or > 1000) return "مكافأة الفائز يجب أن تكون بين 0 و1000."; if (x.SecondPlaceCoins is < 0 or > 500) return "مكافأة المركز الثاني يجب أن تكون بين 0 و500."; if (x.ParticipationCoins is < 0 or > 100) return "مكافأة المشاركة يجب أن تكون بين 0 و100."; if (x.JoinWindowSeconds is < 30 or > 300) return "مدة انتظار الانضمام يجب أن تكون بين 30 و300 ثانية."; if (x.TurnSeconds is < 10 or > 120) return "مدة الدور يجب أن تكون بين 10 و120 ثانية."; return null; }
    private static bool ValidSnowflake(string value) => ulong.TryParse(value, out _);
    private static string CleanUsername(string value, string fallback) => Limit(string.IsNullOrWhiteSpace(value) ? fallback : value.Trim(), 80);
    private static string Limit(string value, int max) => value[..Math.Min(value.Length, max)];
    private sealed record EnabledContext(Guild Guild, PlatformGameDefinition Game, GuildGamesSettings General, GuildGameSetting GameSetting);
    private sealed class PublishPayload { public string HostUsername { get; set; } = string.Empty; public string WinnerUsername { get; set; } = string.Empty; public int MinPlayers { get; set; } public int MaxPlayers { get; set; } public int PlayersCount { get; set; } public int JoinWindowSeconds { get; set; } public int WinnerCoins { get; set; } public int CurrentRound { get; set; } }
}
