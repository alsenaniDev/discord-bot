using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using DiscordBot.Domain.Constants;
using DiscordBot.Domain.Entities;
using DiscordBot.Domain.Enums;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Infrastructure.Services;

public interface IGameHubService
{
    Task<IReadOnlyList<PlatformGameDefinitionDto>> GetCatalogAsync(CancellationToken ct = default);
    Task<PlatformGameDefinitionDto?> GetCatalogGameAsync(Guid id, CancellationToken ct = default);
    Task<GameHubResult<PlatformGameDefinitionDto>> CreateCatalogGameAsync(SavePlatformGameDefinitionRequest request, CancellationToken ct = default);
    Task<GameHubResult<PlatformGameDefinitionDto>> UpdateCatalogGameAsync(Guid id, SavePlatformGameDefinitionRequest request, CancellationToken ct = default);
    Task<PlatformGameDefinitionDto?> ToggleCatalogGameAsync(Guid id, CancellationToken ct = default);
    Task<bool> DisableCatalogGameAsync(Guid id, CancellationToken ct = default);
    Task<GuildGamesSettingsDto?> GetGuildSettingsAsync(Guid guildId, CancellationToken ct = default);
    Task<GameHubResult<GuildGamesSettingsDto>> UpdateGuildSettingsAsync(Guid guildId, UpdateGuildGamesSettingsRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<GuildGameDto>?> GetGuildGamesAsync(Guid guildId, CancellationToken ct = default);
    Task<GameHubResult<GuildGameDto>> UpdateGuildGameAsync(Guid guildId, Guid gameId, UpdateGuildGameSettingRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<GameLeaderboardEntryDto>?> GetLeaderboardAsync(Guid guildId, Guid? gameId, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<GameLeaderboardEntryDto>?> GetLeaderboardByDiscordGuildIdAsync(string discordGuildId, int limit, CancellationToken ct = default);
    Task<GameHubResult<StartGameSessionResponse>> StartSessionAsync(StartGameSessionRequest request, CancellationToken ct = default);
    Task<GameHubResult<CompleteGameSessionResponse>> CompleteSessionAsync(CompleteGameSessionRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<AvailableGameDto>?> GetAvailableGamesAsync(string discordGuildId, CancellationToken ct = default);
    Task<BotGamesContextDto> GetBotContextAsync(string discordGuildId, CancellationToken ct = default);
    Task<GameHubResult<ActivityGamesContextDto>> GetActivityContextAsync(string discordGuildId, string channelDiscordId, string? userDiscordId = null, CancellationToken ct = default);
    Task<GameHubResult<IReadOnlyList<GameLeaderboardEntryDto>>> GetActivityLeaderboardAsync(string discordGuildId, string channelDiscordId, string? gameKey, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<PendingGamePublishActionDto>> GetPendingPublishActionsAsync(CancellationToken ct = default);
    Task<bool> AckPublishActionAsync(Guid id, AckGamePublishActionRequest request, CancellationToken ct = default);
}

public class GameHubService(AppDbContext db, ILogger<GameHubService> logger) : IGameHubService
{
    private static readonly Regex KeyPattern = new("^[a-z0-9][a-z0-9-]{0,63}$", RegexOptions.Compiled);

    public async Task<IReadOnlyList<PlatformGameDefinitionDto>> GetCatalogAsync(CancellationToken ct = default) =>
        (await db.PlatformGameDefinitions.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct)).Select(Map).ToList();

    public async Task<PlatformGameDefinitionDto?> GetCatalogGameAsync(Guid id, CancellationToken ct = default) =>
        (await db.PlatformGameDefinitions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)) is { } game ? Map(game) : null;

    public async Task<GameHubResult<PlatformGameDefinitionDto>> CreateCatalogGameAsync(SavePlatformGameDefinitionRequest request, CancellationToken ct = default)
    {
        var validation = ValidateCatalog(request); if (validation is not null) return GameHubResult<PlatformGameDefinitionDto>.Fail(validation);
        var key = request.Key.Trim().ToLowerInvariant();
        if (await db.PlatformGameDefinitions.AnyAsync(x => x.Key == key, ct)) return GameHubResult<PlatformGameDefinitionDto>.Fail("يوجد لعبة بنفس المفتاح مسبقًا.", 409);
        var game = new PlatformGameDefinition(); Apply(game, request, key); db.PlatformGameDefinitions.Add(game); await db.SaveChangesAsync(ct);
        logger.LogInformation("Platform game {GameKey} ({GameId}) created.", game.Key, game.Id);
        return GameHubResult<PlatformGameDefinitionDto>.Ok(Map(game));
    }

    public async Task<GameHubResult<PlatformGameDefinitionDto>> UpdateCatalogGameAsync(Guid id, SavePlatformGameDefinitionRequest request, CancellationToken ct = default)
    {
        var validation = ValidateCatalog(request); if (validation is not null) return GameHubResult<PlatformGameDefinitionDto>.Fail(validation);
        var game = await db.PlatformGameDefinitions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (game is null) return GameHubResult<PlatformGameDefinitionDto>.Fail("اللعبة غير موجودة.", 404);
        var key = request.Key.Trim().ToLowerInvariant();
        if (await db.PlatformGameDefinitions.AnyAsync(x => x.Id != id && x.Key == key, ct)) return GameHubResult<PlatformGameDefinitionDto>.Fail("يوجد لعبة بنفس المفتاح مسبقًا.", 409);
        Apply(game, request, key); await db.SaveChangesAsync(ct); return GameHubResult<PlatformGameDefinitionDto>.Ok(Map(game));
    }

    public async Task<PlatformGameDefinitionDto?> ToggleCatalogGameAsync(Guid id, CancellationToken ct = default)
    {
        var game = await db.PlatformGameDefinitions.FirstOrDefaultAsync(x => x.Id == id, ct); if (game is null) return null;
        game.IsEnabledGlobally = !game.IsEnabledGlobally; await db.SaveChangesAsync(ct); return Map(game);
    }

    public async Task<bool> DisableCatalogGameAsync(Guid id, CancellationToken ct = default)
    {
        var game = await db.PlatformGameDefinitions.FirstOrDefaultAsync(x => x.Id == id, ct); if (game is null) return false;
        game.IsEnabledGlobally = false; await db.SaveChangesAsync(ct); return true;
    }

    public async Task<GuildGamesSettingsDto?> GetGuildSettingsAsync(Guid guildId, CancellationToken ct = default)
    {
        if (!await db.Guilds.AsNoTracking().AnyAsync(x => x.Id == guildId && x.IsActive, ct)) return null;
        var value = await db.GuildGamesSettings.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == guildId, ct);
        return MapSettings(guildId, value);
    }

    public async Task<GameHubResult<GuildGamesSettingsDto>> UpdateGuildSettingsAsync(Guid guildId, UpdateGuildGamesSettingsRequest request, CancellationToken ct = default)
    {
        if (!await db.Guilds.AnyAsync(x => x.Id == guildId && x.IsActive, ct)) return GameHubResult<GuildGamesSettingsDto>.Fail("السيرفر غير موجود أو غير مربوط.", 404);
        var channelId = string.IsNullOrWhiteSpace(request.GamesChannelDiscordId) ? null : request.GamesChannelDiscordId.Trim();
        if (channelId is not null && (!ulong.TryParse(channelId, out _) || !await db.DiscordChannels.AnyAsync(x => x.GuildId == guildId && x.DiscordChannelId == channelId && x.Type == DiscordChannelType.Text, ct)))
            return GameHubResult<GuildGamesSettingsDto>.Fail("اختر رومًا نصيًا صالحًا من رومات السيرفر.");
        if (request.IsEnabled && channelId is null) return GameHubResult<GuildGamesSettingsDto>.Fail("حدد روم الألعاب قبل تفعيل الميزة.");
        var value = await db.GuildGamesSettings.FirstOrDefaultAsync(x => x.GuildId == guildId, ct);
        if (value is null) { value = new GuildGamesSettings { GuildId = guildId }; db.GuildGamesSettings.Add(value); }
        value.IsEnabled = request.IsEnabled; value.GamesChannelDiscordId = channelId; value.AutoPostPanel = request.AutoPostPanel;
        await db.SaveChangesAsync(ct); return GameHubResult<GuildGamesSettingsDto>.Ok(MapSettings(guildId, value));
    }

    public async Task<IReadOnlyList<GuildGameDto>?> GetGuildGamesAsync(Guid guildId, CancellationToken ct = default)
    {
        if (!await db.Guilds.AsNoTracking().AnyAsync(x => x.Id == guildId && x.IsActive, ct)) return null;
        var plan = await GetGuildPlanAsync(guildId, ct);
        var games = await db.PlatformGameDefinitions.AsNoTracking().Where(x => x.IsEnabledGlobally).OrderBy(x => x.Name).ToListAsync(ct);
        var settings = await db.GuildGameSettings.AsNoTracking().Where(x => x.GuildId == guildId).ToDictionaryAsync(x => x.PlatformGameDefinitionId, ct);
        return games.Select(game => MapGuildGame(game, settings.GetValueOrDefault(game.Id), IsPlanAllowed(plan, game.RequiredPlan))).ToList();
    }

    public async Task<GameHubResult<GuildGameDto>> UpdateGuildGameAsync(Guid guildId, Guid gameId, UpdateGuildGameSettingRequest request, CancellationToken ct = default)
    {
        if (request.PointsPerWin < 0 || request.CooldownSeconds < 0 || request.MaxPlaysPerDay < 0) return GameHubResult<GuildGameDto>.Fail("قيم النقاط ومدة الانتظار والحد اليومي لا يمكن أن تكون سالبة.");
        var game = await db.PlatformGameDefinitions.FirstOrDefaultAsync(x => x.Id == gameId && x.IsEnabledGlobally, ct);
        if (game is null) return GameHubResult<GuildGameDto>.Fail("اللعبة غير موجودة أو غير مفعّلة عالميًا.", 404);
        if (!await db.Guilds.AnyAsync(x => x.Id == guildId && x.IsActive, ct)) return GameHubResult<GuildGameDto>.Fail("السيرفر غير موجود أو غير مربوط.", 404);
        var allowed = IsPlanAllowed(await GetGuildPlanAsync(guildId, ct), game.RequiredPlan);
        if (!allowed) return GameHubResult<GuildGameDto>.Fail("هذه اللعبة غير متاحة في باقة السيرفر.", 403);
        if (request.IsEnabledForGuild && !await db.GuildGamesSettings.AnyAsync(x => x.GuildId == guildId && x.IsEnabled, ct)) return GameHubResult<GuildGameDto>.Fail("فعّل مركز الألعاب وحدد روم الألعاب أولًا.");
        var setting = await db.GuildGameSettings.FirstOrDefaultAsync(x => x.GuildId == guildId && x.PlatformGameDefinitionId == gameId, ct);
        if (setting is null)
        {
            setting = new GuildGameSetting { GuildId = guildId, PlatformGameDefinitionId = gameId, PointsPerWin = game.DefaultPointsPerWin, CooldownSeconds = game.DefaultCooldownSeconds, MaxPlaysPerDay = game.DefaultMaxPlaysPerDay };
            db.GuildGameSettings.Add(setting);
        }
        setting.IsEnabledForGuild = request.IsEnabledForGuild;
        setting.PointsEnabled = game.SupportsScores && request.PointsEnabled; setting.PointsPerWin = game.SupportsScores ? request.PointsPerWin : 0;
        setting.CooldownSeconds = request.CooldownSeconds; setting.MaxPlaysPerDay = request.MaxPlaysPerDay;
        setting.PublishResultAfterGame = game.SupportsResultPublishing && request.PublishResultAfterGame;
        setting.PublishLeaderboardAfterGame = game.SupportsLeaderboard && request.PublishLeaderboardAfterGame;
        setting.PublishOnlyWins = request.PublishOnlyWins;
        await db.SaveChangesAsync(ct); return GameHubResult<GuildGameDto>.Ok(MapGuildGame(game, setting, true));
    }

    public async Task<IReadOnlyList<GameLeaderboardEntryDto>?> GetLeaderboardAsync(Guid guildId, Guid? gameId, int limit, CancellationToken ct = default)
    {
        if (!await db.Guilds.AsNoTracking().AnyAsync(x => x.Id == guildId && x.IsActive, ct)) return null;
        limit = Math.Clamp(limit, 1, 100);
        if (gameId is null)
        {
            var players = await db.GamePlayers.AsNoTracking().Where(x => x.GuildId == guildId).OrderByDescending(x => x.TotalPoints).ThenByDescending(x => x.Wins).Take(limit).ToListAsync(ct);
            return players.Select((x, index) => MapPlayer(x, index + 1)).ToList();
        }
        var game = await db.PlatformGameDefinitions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == gameId && x.SupportsLeaderboard, ct); if (game is null) return [];
        var sessions = await db.GameSessions.AsNoTracking().Where(x => x.GuildId == guildId && x.PlatformGameDefinitionId == gameId && x.Status == "Completed").ToListAsync(ct);
        return sessions.GroupBy(x => x.UserDiscordId).Select(g => new GameLeaderboardEntryDto
        {
            UserDiscordId = g.Key, Username = g.OrderByDescending(x => x.CompletedAt).First().Username ?? g.Key,
            TotalPoints = g.Sum(x => x.PointsAwarded), GamesPlayed = g.Count(), Wins = g.Count(x => x.Won == true), Losses = g.Count(x => x.Won == false)
        }).OrderByDescending(x => x.TotalPoints).ThenByDescending(x => x.Wins).Take(limit).Select((x, i) => { x.Rank = i + 1; return x; }).ToList();
    }

    public async Task<IReadOnlyList<GameLeaderboardEntryDto>?> GetLeaderboardByDiscordGuildIdAsync(string discordGuildId, int limit, CancellationToken ct = default)
    {
        var guildId = await db.Guilds.AsNoTracking().Where(x => x.DiscordGuildId == discordGuildId && x.IsActive).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
        return guildId.HasValue ? await GetLeaderboardAsync(guildId.Value, null, limit, ct) : null;
    }

    public async Task<GameHubResult<StartGameSessionResponse>> StartSessionAsync(StartGameSessionRequest request, CancellationToken ct = default)
    {
        if (!ValidSnowflake(request.GuildDiscordId) || !ValidSnowflake(request.ChannelDiscordId) || !ValidSnowflake(request.UserDiscordId)) return GameHubResult<StartGameSessionResponse>.Fail("بيانات Discord غير صالحة.");
        var guild = await db.Guilds.AsNoTracking().FirstOrDefaultAsync(x => x.DiscordGuildId == request.GuildDiscordId && x.IsActive, ct);
        if (guild is null) return GameHubResult<StartGameSessionResponse>.Fail("هذا السيرفر غير مربوط بمنصة البوت.", 404);
        var general = await db.GuildGamesSettings.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == guild.Id, ct);
        if (general?.IsEnabled != true) return GameHubResult<StartGameSessionResponse>.Fail("الألعاب غير مفعّلة في هذا السيرفر.", 403);
        if (general.GamesChannelDiscordId is null) return GameHubResult<StartGameSessionResponse>.Fail("لم يتم تحديد روم الألعاب بعد.");
        if (general.GamesChannelDiscordId != request.ChannelDiscordId) return GameHubResult<StartGameSessionResponse>.Fail($"🎮 الألعاب متاحة فقط في روم <#{general.GamesChannelDiscordId}>.", 403);
        if (string.IsNullOrWhiteSpace(request.GameKey)) return GameHubResult<StartGameSessionResponse>.Fail("اختر لعبة صالحة.");
        var gameKey = request.GameKey.Trim().ToLowerInvariant();
        var game = await db.PlatformGameDefinitions.AsNoTracking().FirstOrDefaultAsync(x => x.Key == gameKey && x.IsEnabledGlobally, ct);
        if (game is null) return GameHubResult<StartGameSessionResponse>.Fail("اللعبة غير متاحة حاليًا.", 404);
        if (!IsPlanAllowed(await GetGuildPlanAsync(guild.Id, ct), game.RequiredPlan)) return GameHubResult<StartGameSessionResponse>.Fail("هذه اللعبة مقفلة حسب باقة السيرفر.", 403);
        var setting = await db.GuildGameSettings.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == guild.Id && x.PlatformGameDefinitionId == game.Id && x.IsEnabledForGuild, ct);
        if (setting is null) return GameHubResult<StartGameSessionResponse>.Fail("هذه اللعبة غير مفعّلة في السيرفر.", 403);
        var now = DateTimeOffset.UtcNow;
        var active = await db.GameSessions.AnyAsync(x => x.GuildId == guild.Id && x.PlatformGameDefinitionId == game.Id && x.UserDiscordId == request.UserDiscordId && x.Status == "Started" && x.ExpiresAt > now, ct);
        if (active) return GameHubResult<StartGameSessionResponse>.Fail("لديك جلسة نشطة لهذه اللعبة بالفعل.", 409);
        var lastPlayed = await db.GameSessions.Where(x => x.GuildId == guild.Id && x.PlatformGameDefinitionId == game.Id && x.UserDiscordId == request.UserDiscordId).MaxAsync(x => (DateTimeOffset?)x.StartedAt, ct);
        if (lastPlayed.HasValue && setting.CooldownSeconds > 0 && lastPlayed.Value.AddSeconds(setting.CooldownSeconds) > now)
        {
            var seconds = (int)Math.Ceiling((lastPlayed.Value.AddSeconds(setting.CooldownSeconds) - now).TotalSeconds);
            return GameHubResult<StartGameSessionResponse>.Fail($"انتظر {seconds} ثانية قبل المحاولة مرة ثانية.", 429);
        }
        var today = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        if (setting.MaxPlaysPerDay > 0 && await db.GameSessions.CountAsync(x => x.GuildId == guild.Id && x.PlatformGameDefinitionId == game.Id && x.UserDiscordId == request.UserDiscordId && x.StartedAt >= today, ct) >= setting.MaxPlaysPerDay)
            return GameHubResult<StartGameSessionResponse>.Fail("وصلت للحد اليومي لهذه اللعبة. ارجع لنا بكرة!", 429);
        var session = new GameSession { GuildId = guild.Id, PlatformGameDefinitionId = game.Id, UserDiscordId = request.UserDiscordId, ChannelDiscordId = request.ChannelDiscordId, Username = CleanUsername(request.Username, request.UserDiscordId), Status = "Started", StartedAt = now, ExpiresAt = now.AddMinutes(15) };
        db.GameSessions.Add(session); await db.SaveChangesAsync(ct);
        logger.LogInformation("Game session {SessionId} started for game {GameKey}, guild {GuildId}, user {UserId}.", session.Id, game.Key, guild.DiscordGuildId, request.UserDiscordId);
        return GameHubResult<StartGameSessionResponse>.Ok(new StartGameSessionResponse { SessionId = session.Id, GameKey = game.Key, GameName = game.Name, ActivityRoute = game.ActivityRoute, ExpiresAt = session.ExpiresAt });
    }

    public async Task<GameHubResult<CompleteGameSessionResponse>> CompleteSessionAsync(CompleteGameSessionRequest request, CancellationToken ct = default)
    {
        if (!ValidSnowflake(request.GuildDiscordId) || !ValidSnowflake(request.UserDiscordId) || request.Score < 0) return GameHubResult<CompleteGameSessionResponse>.Fail("بيانات نتيجة اللعبة غير صالحة.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var session = await db.GameSessions.Include(x => x.Guild).Include(x => x.PlatformGameDefinition).FirstOrDefaultAsync(x => x.Id == request.SessionId, ct);
        if (session is null) return GameHubResult<CompleteGameSessionResponse>.Fail("جلسة اللعبة غير موجودة.", 404);
        if (session.Status != "Started") return GameHubResult<CompleteGameSessionResponse>.Fail("تم إنهاء هذه الجلسة مسبقًا.", 409);
        var now = DateTimeOffset.UtcNow;
        if (session.ExpiresAt <= now) { session.Status = "Expired"; await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return GameHubResult<CompleteGameSessionResponse>.Fail("انتهت صلاحية جلسة اللعبة.", 410); }
        if (!session.Guild.IsActive || session.Guild.DiscordGuildId != request.GuildDiscordId || session.UserDiscordId != request.UserDiscordId) return GameHubResult<CompleteGameSessionResponse>.Fail("لا تملك صلاحية إنهاء هذه الجلسة.", 403);
        var general = await db.GuildGamesSettings.FirstOrDefaultAsync(x => x.GuildId == session.GuildId && x.IsEnabled, ct);
        var setting = await db.GuildGameSettings.FirstOrDefaultAsync(x => x.GuildId == session.GuildId && x.PlatformGameDefinitionId == session.PlatformGameDefinitionId && x.IsEnabledForGuild, ct);
        if (general is null || setting is null || !session.PlatformGameDefinition.IsEnabledGlobally || !IsPlanAllowed(await GetGuildPlanAsync(session.GuildId, ct), session.PlatformGameDefinition.RequiredPlan)) return GameHubResult<CompleteGameSessionResponse>.Fail("اللعبة لم تعد متاحة لهذا السيرفر.", 403);
        var points = session.PlatformGameDefinition.SupportsScores && setting.PointsEnabled && request.Won ? setting.PointsPerWin : 0;
        session.Status = "Completed"; session.Score = request.Score; session.Won = request.Won; session.PointsAwarded = points; session.CompletedAt = now;
        var player = await db.GamePlayers.FirstOrDefaultAsync(x => x.GuildId == session.GuildId && x.UserDiscordId == session.UserDiscordId, ct);
        if (player is null) { player = new GamePlayer { GuildId = session.GuildId, UserDiscordId = session.UserDiscordId, Username = session.Username ?? session.UserDiscordId }; db.GamePlayers.Add(player); }
        player.Username = CleanUsername(session.Username, session.UserDiscordId); player.TotalPoints += points; player.GamesPlayed++;
        if (request.Won) { player.Wins++; player.CurrentStreak++; player.BestStreak = Math.Max(player.BestStreak, player.CurrentStreak); } else { player.Losses++; player.CurrentStreak = 0; }
        player.LastPlayedAt = now;
        await CreatePublishActionAsync(session, player, general, setting, request.Score, request.Won, ct);
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        logger.LogInformation("Game session {SessionId} completed. Won: {Won}; Points: {Points}.", session.Id, request.Won, points);
        return GameHubResult<CompleteGameSessionResponse>.Ok(new CompleteGameSessionResponse { SessionId = session.Id, PointsAwarded = points, Player = MapPlayer(player, 0) });
    }

    public async Task<IReadOnlyList<AvailableGameDto>?> GetAvailableGamesAsync(string discordGuildId, CancellationToken ct = default)
    {
        var context = await GetBotContextAsync(discordGuildId, ct); return !context.GuildLinked || !context.IsEnabled ? null : context.Games;
    }

    public async Task<BotGamesContextDto> GetBotContextAsync(string discordGuildId, CancellationToken ct = default)
    {
        var guild = await db.Guilds.AsNoTracking().FirstOrDefaultAsync(x => x.DiscordGuildId == discordGuildId && x.IsActive, ct);
        if (guild is null) return new BotGamesContextDto();
        var settings = await db.GuildGamesSettings.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == guild.Id, ct);
        var result = new BotGamesContextDto { GuildLinked = true, IsEnabled = settings?.IsEnabled == true, GamesChannelDiscordId = settings?.GamesChannelDiscordId };
        if (!result.IsEnabled) return result;
        var plan = await GetGuildPlanAsync(guild.Id, ct);
        result.Games = await (from game in db.PlatformGameDefinitions.AsNoTracking()
            join setting in db.GuildGameSettings.AsNoTracking().Where(x => x.GuildId == guild.Id && x.IsEnabledForGuild) on game.Id equals setting.PlatformGameDefinitionId
            where game.IsEnabledGlobally
            orderby game.Name
            select new AvailableGameDto { Id = game.Id, Key = game.Key, Name = game.Name, Description = game.Description, IconUrl = game.IconUrl, ActivityRoute = game.ActivityRoute, PlayMode = game.PlayMode, SupportsScores = game.SupportsScores, SupportsLeaderboard = game.SupportsLeaderboard, RequiredPlanInternal = game.RequiredPlan }).ToListAsync(ct);
        await ApplyPublishedVersionMetadataAsync(result.Games, ct);
        var enabledGames = result.Games;
        result.Games = enabledGames.Where(x => IsPlanAllowed(plan, x.RequiredPlanInternal)).ToList();
        var filteredGames = enabledGames.Where(x => !IsPlanAllowed(plan, x.RequiredPlanInternal)).Select(x => $"{x.Key}:{x.RequiredPlanInternal}").ToArray();
        logger.LogInformation(
            "Games context for guild {DiscordGuildId}: plan {Plan}; guild-enabled games [{EnabledGames}]; returned games [{ReturnedGames}]; filtered by plan [{FilteredGames}].",
            discordGuildId,
            plan,
            string.Join(", ", enabledGames.Select(x => x.Key)),
            string.Join(", ", result.Games.Select(x => x.Key)),
            string.Join(", ", filteredGames));
        return result;
    }

    public async Task<GameHubResult<ActivityGamesContextDto>> GetActivityContextAsync(string discordGuildId, string channelDiscordId, string? userDiscordId = null, CancellationToken ct = default)
    {
        if (!ValidSnowflake(discordGuildId) || !ValidSnowflake(channelDiscordId)) return GameHubResult<ActivityGamesContextDto>.Fail("بيانات السيرفر أو الروم غير صالحة.");
        var guild = await db.Guilds.AsNoTracking().FirstOrDefaultAsync(x => x.DiscordGuildId == discordGuildId && x.IsActive, ct);
        if (guild is null) return GameHubResult<ActivityGamesContextDto>.Fail("هذا السيرفر غير مربوط بمنصة البوت.", 404);
        var settings = await db.GuildGamesSettings.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == guild.Id, ct);
        if (settings?.IsEnabled != true) return GameHubResult<ActivityGamesContextDto>.Fail("الألعاب غير مفعّلة في هذا السيرفر.", 403);
        if (string.IsNullOrWhiteSpace(settings.GamesChannelDiscordId)) return GameHubResult<ActivityGamesContextDto>.Fail("لم يتم تحديد روم الألعاب بعد.", 403);
        if (settings.GamesChannelDiscordId != channelDiscordId) return GameHubResult<ActivityGamesContextDto>.Fail($"🎮 الألعاب متاحة فقط في روم <#{settings.GamesChannelDiscordId}>.", 403);
        var botContext = await GetBotContextAsync(discordGuildId, ct);
        if (!string.IsNullOrWhiteSpace(userDiscordId)) await ApplySandboxVersionsAsync(botContext.Games, guild.Id, discordGuildId, userDiscordId, ct);
        var leaderboard = await GetLeaderboardAsync(guild.Id, null, 10, ct) ?? [];
        return GameHubResult<ActivityGamesContextDto>.Ok(new ActivityGamesContextDto { GuildDiscordId = discordGuildId, ChannelDiscordId = channelDiscordId, GamesChannelDiscordId = settings.GamesChannelDiscordId, Games = botContext.Games, Leaderboard = leaderboard });
    }

    public async Task<GameHubResult<IReadOnlyList<GameLeaderboardEntryDto>>> GetActivityLeaderboardAsync(string discordGuildId, string channelDiscordId, string? gameKey, int limit, CancellationToken ct = default)
    {
        var context = await GetActivityContextAsync(discordGuildId, channelDiscordId, null, ct);
        if (!context.Succeeded) return GameHubResult<IReadOnlyList<GameLeaderboardEntryDto>>.Fail(context.Error!, context.StatusCode);
        Guid? gameId = null;
        if (!string.IsNullOrWhiteSpace(gameKey))
        {
            var game = context.Value!.Games.FirstOrDefault(x => x.Key.Equals(gameKey.Trim(), StringComparison.OrdinalIgnoreCase));
            if (game is null) return GameHubResult<IReadOnlyList<GameLeaderboardEntryDto>>.Fail("هذه اللعبة غير متاحة في الباقة الحالية.", 403);
            if (!game.SupportsLeaderboard) return GameHubResult<IReadOnlyList<GameLeaderboardEntryDto>>.Fail("هذه اللعبة لا تدعم الترتيب.");
            gameId = game.Id;
        }
        var guildId = await db.Guilds.AsNoTracking().Where(x => x.DiscordGuildId == discordGuildId && x.IsActive).Select(x => x.Id).FirstAsync(ct);
        return GameHubResult<IReadOnlyList<GameLeaderboardEntryDto>>.Ok(await GetLeaderboardAsync(guildId, gameId, limit, ct) ?? []);
    }

    private async Task ApplyPublishedVersionMetadataAsync(List<AvailableGameDto> games, CancellationToken ct)
    {
        if (games.Count == 0) return;
        var gameIds = games.Select(x => x.Id).ToHashSet();
        var versions = await db.GameVersions.AsNoTracking()
            .Where(x => gameIds.Contains(x.GameDefinitionId) && x.Status == "Published")
            .OrderByDescending(x => x.PublishedAt).ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
        foreach (var game in games)
        {
            var version = versions.FirstOrDefault(x => x.GameDefinitionId == game.Id);
            if (version is null) continue;
            ApplyVersion(game, version, false);
        }
    }

    private async Task ApplySandboxVersionsAsync(List<AvailableGameDto> games, Guid guildId, string discordGuildId, string userDiscordId, CancellationToken ct)
    {
        if (games.Count == 0) return;
        var gameIds = games.Select(x => x.Id).ToHashSet();
        var sandboxVersions = await db.GameVersions.AsNoTracking().Include(x => x.SandboxAccess)
            .Where(x => gameIds.Contains(x.GameDefinitionId) && x.Status == "Sandbox" && x.SandboxAccess.Any(a => a.GuildDiscordId == discordGuildId && (a.UserDiscordId == null || a.UserDiscordId == userDiscordId)))
            .OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        foreach (var version in sandboxVersions)
        {
            var game = games.FirstOrDefault(x => x.Id == version.GameDefinitionId);
            if (game is null) continue;
            ApplyVersion(game, version, true);
        }
        if (sandboxVersions.Count > 0)
            logger.LogInformation("Applied {Count} sandbox game versions for guild {GuildId}, user {UserId}.", sandboxVersions.Count, guildId, userDiscordId);
    }

    private static void ApplyVersion(AvailableGameDto game, GameVersion version, bool sandbox)
    {
        game.GameVersionId = version.Id;
        game.ActivityRoute = string.IsNullOrWhiteSpace(version.ActivityRoute) ? game.ActivityRoute : version.ActivityRoute;
        game.FrontendUrl = version.FrontendUrl;
        game.BackendUrl = version.BackendUrl;
        game.EngineType = ManifestString(version.ManifestJson, "engineType") ?? game.EngineType;
        game.FrontendMode = ManifestString(version.ManifestJson, "frontendMode") ?? game.FrontendMode;
        game.IsSandbox = sandbox;
        game.SandboxWarning = sandbox ? "هذه نسخة تجريبية وقد تحتوي على أخطاء." : null;
    }

    private static string? ManifestString(string json, string property)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty(property, out var node) && node.ValueKind == JsonValueKind.String ? node.GetString() : null;
        }
        catch { return null; }
    }

    public async Task<IReadOnlyList<PendingGamePublishActionDto>> GetPendingPublishActionsAsync(CancellationToken ct = default)
    {
        var invalidActions = await db.GameResultPublishActions.Include(x => x.Guild).ThenInclude(x => x.GamesSettings)
            .Where(x => x.Status == "Pending" && (x.Guild.GamesSettings == null || !x.Guild.GamesSettings.IsEnabled || x.Guild.GamesSettings.GamesChannelDiscordId != x.ChannelDiscordId))
            .Take(100).ToListAsync(ct);
        foreach (var action in invalidActions) { action.Status = "Failed"; action.ErrorMessage = "تم تغيير روم الألعاب أو تعطيل الميزة قبل النشر."; action.ProcessedAt = DateTimeOffset.UtcNow; }
        if (invalidActions.Count > 0) await db.SaveChangesAsync(ct);
        var actions = await db.GameResultPublishActions.AsNoTracking().Include(x => x.Guild).ThenInclude(x => x.GamesSettings)
            .Where(x => x.Status == "Pending" && x.Guild.GamesSettings != null && x.Guild.GamesSettings.IsEnabled && x.Guild.GamesSettings.GamesChannelDiscordId == x.ChannelDiscordId)
            .OrderBy(x => x.CreatedAt).Take(100).ToListAsync(ct);
        logger.LogInformation("Returning {Count} pending game publish actions.", actions.Count);
        return actions.Select(x => new PendingGamePublishActionDto { Id = x.Id, GameSessionId = x.GameSessionId, DiscordGuildId = x.Guild.DiscordGuildId, ChannelDiscordId = x.ChannelDiscordId, Type = x.Type, Content = JsonSerializer.Deserialize<GamePublishPayload>(x.PayloadJson)?.Content ?? string.Empty }).ToList();
    }

    public async Task<bool> AckPublishActionAsync(Guid id, AckGamePublishActionRequest request, CancellationToken ct = default)
    {
        var action = await db.GameResultPublishActions.FirstOrDefaultAsync(x => x.Id == id && x.Status == "Pending", ct); if (action is null) return false;
        action.Status = request.Success ? "Processed" : "Failed"; action.ErrorMessage = request.Success ? null : (request.ErrorMessage ?? "تعذر نشر نتيجة اللعبة.")[..Math.Min((request.ErrorMessage ?? "تعذر نشر نتيجة اللعبة.").Length, 2000)]; action.ProcessedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct); return true;
    }

    private async Task CreatePublishActionAsync(GameSession session, GamePlayer player, GuildGamesSettings general, GuildGameSetting setting, int score, bool won, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(general.GamesChannelDiscordId)) return;
        var publishResult = session.PlatformGameDefinition.SupportsResultPublishing && setting.PublishResultAfterGame && (!setting.PublishOnlyWins || won);
        var publishLeaderboard = session.PlatformGameDefinition.SupportsLeaderboard && setting.PublishLeaderboardAfterGame;
        if (!publishResult && !publishLeaderboard) return;
        var parts = new List<string>();
        if (publishResult)
        {
            parts.Add(won
                ? $"🎮 نتيجة {session.PlatformGameDefinition.Name}\n\n🏆 {player.Username} فاز وحصل على {session.PointsAwarded} نقطة!\n⭐ مجموع النقاط: {player.TotalPoints}\n🔥 سلسلة الفوز الحالية: {player.CurrentStreak}"
                : $"🎮 نتيجة {session.PlatformGameDefinition.Name}\n\n{player.Username} أنهى اللعبة.\nالنتيجة: {score}\n⭐ مجموع النقاط: {player.TotalPoints}");
        }
        if (publishLeaderboard)
        {
            await db.SaveChangesAsync(ct);
            var leaders = await db.GamePlayers.AsNoTracking().Where(x => x.GuildId == session.GuildId).OrderByDescending(x => x.TotalPoints).ThenByDescending(x => x.Wins).Take(10).ToListAsync(ct);
            parts.Add("🏆 ترتيب " + session.PlatformGameDefinition.Name + "\n\n" + string.Join('\n', leaders.Select((x, i) => $"{i + 1}. {x.Username} — {x.TotalPoints} نقطة")));
        }
        db.GameResultPublishActions.Add(new GameResultPublishAction { GuildId = session.GuildId, GameSessionId = session.Id, ChannelDiscordId = general.GamesChannelDiscordId, Type = publishResult && publishLeaderboard ? "ResultAndLeaderboard" : publishLeaderboard ? "Leaderboard" : "Result", PayloadJson = JsonSerializer.Serialize(new GamePublishPayload { Content = string.Join("\n\n", parts) }) });
    }

    private async Task<string> GetGuildPlanAsync(Guid guildId, CancellationToken ct)
    {
        var subscription = await db.GuildSubscriptions.AsNoTracking().Include(x => x.SubscriptionPlan).FirstOrDefaultAsync(x => x.GuildId == guildId, ct);
        return subscription is not null && subscription.Status == GuildSubscriptionStatus.Active && (!subscription.ExpiresAt.HasValue || subscription.ExpiresAt > DateTimeOffset.UtcNow) ? subscription.SubscriptionPlan.Key : PlanKeys.Free;
    }

    private static bool IsPlanAllowed(string actual, string required)
    {
        var rank = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { [PlanKeys.Free] = 0, [PlanKeys.Basic] = 1, [PlanKeys.Pro] = 2, [PlanKeys.Premium] = 3 };
        return rank.TryGetValue(actual, out var actualRank) && rank.TryGetValue(required.Trim(), out var requiredRank) ? actualRank >= requiredRank : actual.Equals(required.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string? ValidateCatalog(SavePlatformGameDefinitionRequest x)
    {
        if (string.IsNullOrWhiteSpace(x.Key) || !KeyPattern.IsMatch(x.Key.Trim().ToLowerInvariant())) return "مفتاح اللعبة مطلوب، ويقبل الحروف الإنجليزية الصغيرة والأرقام والشرطة فقط.";
        if (string.IsNullOrWhiteSpace(x.Name)) return "اسم اللعبة مطلوب.";
        if (string.IsNullOrWhiteSpace(x.ActivityRoute) || !x.ActivityRoute.Trim().StartsWith('/')) return "مسار اللعبة الداخلي مطلوب ويجب أن يبدأ بـ /.";
        if (!Enum.IsDefined(x.PlayMode)) return "نوع اللعبة يجب أن يكون Solo أو Multiplayer.";
        if (x.DefaultPointsPerWin < 0 || x.DefaultCooldownSeconds < 0 || x.DefaultMaxPlaysPerDay < 0) return "القيم الافتراضية لا يمكن أن تكون سالبة.";
        return null;
    }

    private static void Apply(PlatformGameDefinition game, SavePlatformGameDefinitionRequest x, string key)
    {
        game.Key = key; game.Name = x.Name.Trim(); game.Description = Clean(x.Description); game.IconUrl = Clean(x.IconUrl); game.ActivityRoute = x.ActivityRoute.Trim(); game.RequiredPlan = string.IsNullOrWhiteSpace(x.RequiredPlan) ? PlanKeys.Free : x.RequiredPlan.Trim().ToLowerInvariant(); game.PlayMode = x.PlayMode;
        game.IsEnabledGlobally = x.IsEnabledGlobally; game.DefaultPointsPerWin = x.DefaultPointsPerWin; game.DefaultCooldownSeconds = x.DefaultCooldownSeconds; game.DefaultMaxPlaysPerDay = x.DefaultMaxPlaysPerDay;
        game.SupportsScores = x.SupportsScores; game.SupportsLeaderboard = x.SupportsLeaderboard; game.SupportsResultPublishing = x.SupportsResultPublishing;
    }

    private static PlatformGameDefinitionDto Map(PlatformGameDefinition x) => new() { Id = x.Id, Key = x.Key, Name = x.Name, Description = x.Description, IconUrl = x.IconUrl, ActivityRoute = x.ActivityRoute, RequiredPlan = x.RequiredPlan, PlayMode = x.PlayMode, IsEnabledGlobally = x.IsEnabledGlobally, DefaultPointsPerWin = x.DefaultPointsPerWin, DefaultCooldownSeconds = x.DefaultCooldownSeconds, DefaultMaxPlaysPerDay = x.DefaultMaxPlaysPerDay, SupportsScores = x.SupportsScores, SupportsLeaderboard = x.SupportsLeaderboard, SupportsResultPublishing = x.SupportsResultPublishing, CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt };
    private static GuildGamesSettingsDto MapSettings(Guid guildId, GuildGamesSettings? x) => new() { GuildId = guildId, IsEnabled = x?.IsEnabled ?? false, GamesChannelDiscordId = x?.GamesChannelDiscordId, AutoPostPanel = x?.AutoPostPanel ?? false, GamesPanelMessageDiscordId = x?.GamesPanelMessageDiscordId };
    private static GuildGameDto MapGuildGame(PlatformGameDefinition game, GuildGameSetting? x, bool allowed) => new() { Id = game.Id, Key = game.Key, Name = game.Name, Description = game.Description, IconUrl = game.IconUrl, ActivityRoute = game.ActivityRoute, RequiredPlan = game.RequiredPlan, PlayMode = game.PlayMode, IsEnabledGlobally = game.IsEnabledGlobally, DefaultPointsPerWin = game.DefaultPointsPerWin, DefaultCooldownSeconds = game.DefaultCooldownSeconds, DefaultMaxPlaysPerDay = game.DefaultMaxPlaysPerDay, SupportsScores = game.SupportsScores, SupportsLeaderboard = game.SupportsLeaderboard, SupportsResultPublishing = game.SupportsResultPublishing, CreatedAt = game.CreatedAt, UpdatedAt = game.UpdatedAt, IsAvailableByPlan = allowed, IsEnabledForGuild = x?.IsEnabledForGuild ?? false, PointsEnabled = game.SupportsScores && (x?.PointsEnabled ?? true), PointsPerWin = game.SupportsScores ? x?.PointsPerWin ?? game.DefaultPointsPerWin : 0, CooldownSeconds = x?.CooldownSeconds ?? game.DefaultCooldownSeconds, MaxPlaysPerDay = x?.MaxPlaysPerDay ?? game.DefaultMaxPlaysPerDay, PublishResultAfterGame = game.SupportsResultPublishing && (x?.PublishResultAfterGame ?? true), PublishLeaderboardAfterGame = game.SupportsLeaderboard && (x?.PublishLeaderboardAfterGame ?? false), PublishOnlyWins = x?.PublishOnlyWins ?? false, LockedReason = allowed ? null : "مقفلة حسب باقة السيرفر" };
    private static GameLeaderboardEntryDto MapPlayer(GamePlayer x, int rank) => new() { Rank = rank, UserDiscordId = x.UserDiscordId, Username = x.Username, TotalPoints = x.TotalPoints, GamesPlayed = x.GamesPlayed, Wins = x.Wins, Losses = x.Losses, CurrentStreak = x.CurrentStreak, BestStreak = x.BestStreak };
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string CleanUsername(string? value, string fallback) => (Clean(value) ?? fallback)[..Math.Min((Clean(value) ?? fallback).Length, 80)];
    private static bool ValidSnowflake(string value) => ulong.TryParse(value, out _);
    private sealed class GamePublishPayload { public string Content { get; set; } = string.Empty; }
}
