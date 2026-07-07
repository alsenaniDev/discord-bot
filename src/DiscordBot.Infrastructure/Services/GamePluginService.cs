using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DiscordBot.Domain.Entities;
using DiscordBot.Domain.Enums;
using DiscordBot.Infrastructure.Auth;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Infrastructure.Services;

public interface IGamePluginService
{
    Task<IReadOnlyList<GameVersionDto>> GetVersionsAsync(Guid gameId, CancellationToken ct = default);
    Task<GameHubResult<GameVersionDto>> CreateVersionAsync(Guid gameId, CreateGameVersionRequest request, CancellationToken ct = default);
    Task<GameHubResult<GameVersionDto>> UpdateVersionStatusAsync(Guid versionId, UpdateGameVersionStatusRequest request, CancellationToken ct = default);
    Task<GameHubResult<GameSandboxAccessDto>> AddSandboxAccessAsync(Guid versionId, AddGameSandboxAccessRequest request, CancellationToken ct = default);
    Task<bool> RemoveSandboxAccessAsync(Guid accessId, CancellationToken ct = default);
    Task<GameHubResult<IssueGameRuntimeTokenResponse>> IssueRuntimeTokenAsync(IssueGameRuntimeTokenRequest request, ActivityDiscordUser user, CancellationToken ct = default);
    Task<GameHubResult<GameRuntimeContextDto>> ValidateRuntimeTokenAsync(string runtimeToken, CancellationToken ct = default);
    Task<GameHubResult<GameIntegrationWalletDto>> GetWalletAsync(string runtimeToken, CancellationToken ct = default);
    Task<GameHubResult<GameIntegrationWalletDto>> RequestWalletTransactionAsync(string runtimeToken, RequestGameWalletTransactionRequest request, CancellationToken ct = default);
    Task<GameHubResult<GameEventDto>> EmitEventAsync(string runtimeToken, EmitGameEventRequest request, CancellationToken ct = default);
    Task<GameHubResult<GameBotPublishActionDto>> RequestBotPublishAsync(string runtimeToken, RequestGameBotPublishRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<PendingGameBotPublishActionDto>> GetPendingBotPublishActionsAsync(CancellationToken ct = default);
    Task<bool> AckBotPublishActionAsync(Guid id, AckGameBotPublishActionRequest request, CancellationToken ct = default);
}

public class GamePluginService(AppDbContext db, ILogger<GamePluginService> logger) : IGamePluginService
{
    private static readonly Regex VersionPattern = new("^[0-9A-Za-z][0-9A-Za-z._-]{0,39}$", RegexOptions.Compiled);
    private static readonly HashSet<string> VersionStatuses = ["Draft", "Sandbox", "InReview", "Published", "Rejected", "Disabled"];

    public async Task<IReadOnlyList<GameVersionDto>> GetVersionsAsync(Guid gameId, CancellationToken ct = default)
    {
        return await db.GameVersions.AsNoTracking().Include(x => x.GameDefinition).Include(x => x.SandboxAccess)
            .Where(x => x.GameDefinitionId == gameId).OrderByDescending(x => x.CreatedAt).Select(x => MapVersion(x)).ToListAsync(ct);
    }

    public async Task<GameHubResult<GameVersionDto>> CreateVersionAsync(Guid gameId, CreateGameVersionRequest request, CancellationToken ct = default)
    {
        var game = await db.PlatformGameDefinitions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == gameId, ct);
        if (game is null) return GameHubResult<GameVersionDto>.Fail("اللعبة غير موجودة.", 404);
        var validation = ValidateVersionRequest(request);
        if (validation is not null) return GameHubResult<GameVersionDto>.Fail(validation);
        if (await db.GameVersions.AnyAsync(x => x.GameDefinitionId == gameId && x.Version == request.Version.Trim(), ct))
            return GameHubResult<GameVersionDto>.Fail("يوجد إصدار بنفس الرقم لهذه اللعبة.", 409);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (request.Status == "Published") await DisableOtherPublishedAsync(gameId, ct);
        var now = DateTimeOffset.UtcNow;
        var version = new GameVersion
        {
            GameDefinitionId = gameId,
            Version = request.Version.Trim(),
            Status = request.Status,
            FrontendUrl = TrimNullable(request.FrontendUrl),
            BackendUrl = TrimNullable(request.BackendUrl),
            ActivityRoute = TrimNullable(request.ActivityRoute) ?? game.ActivityRoute,
            ManifestJson = NormalizeJson(request.ManifestJson),
            Notes = TrimNullable(request.Notes),
            PublishedAt = request.Status == "Published" ? now : null
        };
        db.GameVersions.Add(version);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        version.GameDefinition = game;
        logger.LogInformation("Game version {VersionId} for {GameKey} created with status {Status}.", version.Id, game.Key, version.Status);
        return GameHubResult<GameVersionDto>.Ok(MapVersion(version));
    }

    public async Task<GameHubResult<GameVersionDto>> UpdateVersionStatusAsync(Guid versionId, UpdateGameVersionStatusRequest request, CancellationToken ct = default)
    {
        if (!VersionStatuses.Contains(request.Status)) return GameHubResult<GameVersionDto>.Fail("حالة الإصدار غير صالحة.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var version = await db.GameVersions.Include(x => x.GameDefinition).Include(x => x.SandboxAccess).FirstOrDefaultAsync(x => x.Id == versionId, ct);
        if (version is null) return GameHubResult<GameVersionDto>.Fail("الإصدار غير موجود.", 404);
        if (request.Status == "Published") await DisableOtherPublishedAsync(version.GameDefinitionId, ct);
        version.Status = request.Status;
        version.PublishedAt = request.Status == "Published" ? DateTimeOffset.UtcNow : version.PublishedAt;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return GameHubResult<GameVersionDto>.Ok(MapVersion(version));
    }

    public async Task<GameHubResult<GameSandboxAccessDto>> AddSandboxAccessAsync(Guid versionId, AddGameSandboxAccessRequest request, CancellationToken ct = default)
    {
        if (!ValidSnowflake(request.GuildDiscordId) || (!string.IsNullOrWhiteSpace(request.UserDiscordId) && !ValidSnowflake(request.UserDiscordId)))
            return GameHubResult<GameSandboxAccessDto>.Fail("بيانات سيرفر أو مستخدم Discord غير صالحة.");
        if (!await db.GameVersions.AnyAsync(x => x.Id == versionId, ct)) return GameHubResult<GameSandboxAccessDto>.Fail("الإصدار غير موجود.", 404);
        var guildId = request.GuildDiscordId.Trim();
        var userId = TrimNullable(request.UserDiscordId);
        var existing = await db.GameSandboxAccess.FirstOrDefaultAsync(x => x.GameVersionId == versionId && x.GuildDiscordId == guildId && x.UserDiscordId == userId, ct);
        if (existing is not null) return GameHubResult<GameSandboxAccessDto>.Ok(MapSandbox(existing));
        var access = new GameSandboxAccess { GameVersionId = versionId, GuildDiscordId = guildId, UserDiscordId = userId };
        db.GameSandboxAccess.Add(access); await db.SaveChangesAsync(ct);
        return GameHubResult<GameSandboxAccessDto>.Ok(MapSandbox(access));
    }

    public async Task<bool> RemoveSandboxAccessAsync(Guid accessId, CancellationToken ct = default)
    {
        var access = await db.GameSandboxAccess.FirstOrDefaultAsync(x => x.Id == accessId, ct);
        if (access is null) return false;
        db.GameSandboxAccess.Remove(access); await db.SaveChangesAsync(ct); return true;
    }

    public async Task<GameHubResult<IssueGameRuntimeTokenResponse>> IssueRuntimeTokenAsync(IssueGameRuntimeTokenRequest request, ActivityDiscordUser user, CancellationToken ct = default)
    {
        if (!ValidSnowflake(request.GuildDiscordId) || !ValidSnowflake(request.ChannelDiscordId)) return GameHubResult<IssueGameRuntimeTokenResponse>.Fail("بيانات السيرفر أو الروم غير صالحة.");
        var guild = await db.Guilds.AsNoTracking().FirstOrDefaultAsync(x => x.DiscordGuildId == request.GuildDiscordId && x.IsActive, ct);
        if (guild is null) return GameHubResult<IssueGameRuntimeTokenResponse>.Fail("هذا السيرفر غير مربوط بمنصة البوت.", 404);
        var settings = await db.GuildGamesSettings.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == guild.Id, ct);
        if (settings?.IsEnabled != true) return GameHubResult<IssueGameRuntimeTokenResponse>.Fail("الألعاب غير مفعّلة في هذا السيرفر.", 403);
        if (settings.GamesChannelDiscordId != request.ChannelDiscordId) return GameHubResult<IssueGameRuntimeTokenResponse>.Fail($"🎮 الألعاب متاحة فقط في روم <#{settings.GamesChannelDiscordId}>.", 403);
        var key = request.GameKey.Trim().ToLowerInvariant();
        var game = await db.PlatformGameDefinitions.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key && x.IsEnabledGlobally, ct);
        if (game is null) return GameHubResult<IssueGameRuntimeTokenResponse>.Fail("اللعبة غير متاحة حاليًا.", 404);
        if (!await db.GuildGameSettings.AsNoTracking().AnyAsync(x => x.GuildId == guild.Id && x.PlatformGameDefinitionId == game.Id && x.IsEnabledForGuild, ct))
            return GameHubResult<IssueGameRuntimeTokenResponse>.Fail("هذه اللعبة غير مفعّلة في السيرفر.", 403);
        if (!IsPlanAllowed(await GetGuildPlanAsync(guild.Id, ct), game.RequiredPlan)) return GameHubResult<IssueGameRuntimeTokenResponse>.Fail("هذه اللعبة مقفلة حسب باقة السيرفر.", 403);
        var sandbox = await db.GameVersions.AsNoTracking()
            .Where(x => x.GameDefinitionId == game.Id && x.Status == "Sandbox" && x.SandboxAccess.Any(a => a.GuildDiscordId == request.GuildDiscordId && (a.UserDiscordId == null || a.UserDiscordId == user.Id)))
            .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        var version = sandbox ?? await db.GameVersions.AsNoTracking().Where(x => x.GameDefinitionId == game.Id && x.Status == "Published").OrderByDescending(x => x.PublishedAt).FirstOrDefaultAsync(ct);
        if (version is null) return GameHubResult<IssueGameRuntimeTokenResponse>.Fail("لا يوجد إصدار منشور لهذه اللعبة.", 404);
        var raw = GenerateToken();
        var expires = DateTimeOffset.UtcNow.AddMinutes(15);
        db.GameRuntimeTokens.Add(new GameRuntimeToken
        {
            TokenHash = HashToken(raw),
            GameKey = game.Key,
            GameVersionId = version.Id,
            GuildId = guild.Id,
            GuildDiscordId = guild.DiscordGuildId,
            ChannelDiscordId = request.ChannelDiscordId,
            UserDiscordId = user.Id,
            ExpiresAt = expires,
            Mode = version.Status == "Sandbox" ? "Sandbox" : "Production"
        });
        await db.SaveChangesAsync(ct);
        return GameHubResult<IssueGameRuntimeTokenResponse>.Ok(new IssueGameRuntimeTokenResponse { RuntimeToken = raw, ExpiresAt = expires, Mode = version.Status == "Sandbox" ? "Sandbox" : "Production", GameVersionId = version.Id });
    }

    public async Task<GameHubResult<GameRuntimeContextDto>> ValidateRuntimeTokenAsync(string runtimeToken, CancellationToken ct = default)
    {
        var token = await LoadTokenAsync(runtimeToken, ct);
        return token is null
            ? GameHubResult<GameRuntimeContextDto>.Fail("رمز تشغيل اللعبة غير صالح أو منتهي.", 401)
            : GameHubResult<GameRuntimeContextDto>.Ok(MapRuntime(token));
    }

    public async Task<GameHubResult<GameIntegrationWalletDto>> GetWalletAsync(string runtimeToken, CancellationToken ct = default)
    {
        var token = await LoadTokenAsync(runtimeToken, ct);
        if (token is null) return GameHubResult<GameIntegrationWalletDto>.Fail("رمز تشغيل اللعبة غير صالح أو منتهي.", 401);
        var balance = await db.GameWallets.AsNoTracking().Where(x => x.GuildId == token.GuildId && x.UserDiscordId == token.UserDiscordId).Select(x => (int?)x.Balance).FirstOrDefaultAsync(ct) ?? 0;
        return GameHubResult<GameIntegrationWalletDto>.Ok(new GameIntegrationWalletDto { Balance = balance });
    }

    public async Task<GameHubResult<GameIntegrationWalletDto>> RequestWalletTransactionAsync(string runtimeToken, RequestGameWalletTransactionRequest request, CancellationToken ct = default)
    {
        var token = await LoadTokenAsync(runtimeToken, ct);
        if (token is null) return GameHubResult<GameIntegrationWalletDto>.Fail("رمز تشغيل اللعبة غير صالح أو منتهي.", 401);
        if (request.Amount >= 0) return GameHubResult<GameIntegrationWalletDto>.Fail("لا يمكن للألعاب إضافة عملات مباشرة. استخدم حدث لعبة يخضع لقواعد المنصة.", 403);
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey)) return GameHubResult<GameIntegrationWalletDto>.Fail("مفتاح منع التكرار مطلوب.");
        var referenceId = StableGuid($"{token.GameKey}:wallet:{request.IdempotencyKey}");
        var existing = await db.GameWalletTransactions.AsNoTracking().AnyAsync(x => x.ReferenceId == referenceId && x.UserDiscordId == token.UserDiscordId && x.Type == request.Type, ct);
        if (existing) return await GetWalletAsync(runtimeToken, ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var wallet = await db.GameWallets.FirstOrDefaultAsync(x => x.GuildId == token.GuildId && x.UserDiscordId == token.UserDiscordId, ct);
        if (wallet is null) { wallet = new GameWallet { GuildId = token.GuildId, UserDiscordId = token.UserDiscordId }; db.GameWallets.Add(wallet); }
        if (wallet.Balance + request.Amount < 0) return GameHubResult<GameIntegrationWalletDto>.Fail("رصيد العملات غير كافٍ.", 409);
        wallet.Balance += request.Amount;
        db.GameWalletTransactions.Add(new GameWalletTransaction { GuildId = token.GuildId, UserDiscordId = token.UserDiscordId, Amount = request.Amount, Type = Clean(request.Type, 64, "game.debit"), Reason = Clean(request.Reason, 500, "خصم من لعبة"), ReferenceId = referenceId });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return GameHubResult<GameIntegrationWalletDto>.Ok(new GameIntegrationWalletDto { Balance = wallet.Balance });
    }

    public async Task<GameHubResult<GameEventDto>> EmitEventAsync(string runtimeToken, EmitGameEventRequest request, CancellationToken ct = default)
    {
        var token = await LoadTokenAsync(runtimeToken, ct);
        if (token is null) return GameHubResult<GameEventDto>.Fail("رمز تشغيل اللعبة غير صالح أو منتهي.", 401);
        var validation = ValidateEventRequest(request);
        if (validation is not null) return GameHubResult<GameEventDto>.Fail(validation);
        var existing = await db.GameEvents.AsNoTracking().FirstOrDefaultAsync(x => x.GameKey == token.GameKey && x.IdempotencyKey == request.IdempotencyKey, ct);
        if (existing is not null) return GameHubResult<GameEventDto>.Ok(MapEvent(existing));
        var gameEvent = new GameEvent { GameKey = token.GameKey, GameVersionId = token.GameVersionId, GuildId = token.GuildId, GuildDiscordId = token.GuildDiscordId, ChannelDiscordId = token.ChannelDiscordId, UserDiscordId = token.UserDiscordId, EventType = request.EventType.Trim(), PayloadJson = NormalizeJson(request.PayloadJson), IdempotencyKey = request.IdempotencyKey.Trim() };
        db.GameEvents.Add(gameEvent); await db.SaveChangesAsync(ct);
        return GameHubResult<GameEventDto>.Ok(MapEvent(gameEvent));
    }

    public async Task<GameHubResult<GameBotPublishActionDto>> RequestBotPublishAsync(string runtimeToken, RequestGameBotPublishRequest request, CancellationToken ct = default)
    {
        var token = await LoadTokenAsync(runtimeToken, ct);
        if (token is null) return GameHubResult<GameBotPublishActionDto>.Fail("رمز تشغيل اللعبة غير صالح أو منتهي.", 401);
        if (!ValidSnowflake(request.ChannelDiscordId)) return GameHubResult<GameBotPublishActionDto>.Fail("روم النشر غير صالح.");
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey)) return GameHubResult<GameBotPublishActionDto>.Fail("مفتاح منع التكرار مطلوب.");
        if (!IsValidJson(request.MessageJson)) return GameHubResult<GameBotPublishActionDto>.Fail("صيغة رسالة النشر غير صالحة.");
        var eventResult = await EmitEventAsync(runtimeToken, new EmitGameEventRequest { EventType = "game.result.publish_requested", PayloadJson = "{\"source\":\"bot.publish\"}", IdempotencyKey = $"publish:{request.IdempotencyKey.Trim()}" }, ct);
        if (!eventResult.Succeeded) return GameHubResult<GameBotPublishActionDto>.Fail(eventResult.Error!, eventResult.StatusCode);
        var existing = await db.GameBotPublishActions.AsNoTracking().FirstOrDefaultAsync(x => x.GameEventId == eventResult.Value!.Id, ct);
        if (existing is not null) return GameHubResult<GameBotPublishActionDto>.Ok(MapPublish(existing));
        var action = new GameBotPublishAction { GameEventId = eventResult.Value!.Id, GuildId = token.GuildId, ChannelDiscordId = request.ChannelDiscordId.Trim(), MessageJson = NormalizeJson(request.MessageJson) };
        db.GameBotPublishActions.Add(action); await db.SaveChangesAsync(ct);
        return GameHubResult<GameBotPublishActionDto>.Ok(MapPublish(action));
    }

    public async Task<IReadOnlyList<PendingGameBotPublishActionDto>> GetPendingBotPublishActionsAsync(CancellationToken ct = default)
    {
        var actions = await db.GameBotPublishActions.AsNoTracking().Include(x => x.Guild)
            .Where(x => x.Status == "Pending")
            .OrderBy(x => x.CreatedAt).Take(100).ToListAsync(ct);
        logger.LogInformation("Returning {Count} pending generic game bot publish actions.", actions.Count);
        return actions.Select(MapPendingPublish).ToList();
    }

    public async Task<bool> AckBotPublishActionAsync(Guid id, AckGameBotPublishActionRequest request, CancellationToken ct = default)
    {
        var action = await db.GameBotPublishActions.FirstOrDefaultAsync(x => x.Id == id && x.Status == "Pending", ct);
        if (action is null) return false;
        action.Status = request.Success ? "Processed" : "Failed";
        action.ProcessedAt = DateTimeOffset.UtcNow;
        action.ErrorMessage = request.Success ? null : Clean(request.ErrorMessage, 2000, "تعذر نشر رسالة اللعبة.");
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task DisableOtherPublishedAsync(Guid gameId, CancellationToken ct)
    {
        var published = await db.GameVersions.Where(x => x.GameDefinitionId == gameId && x.Status == "Published").ToListAsync(ct);
        foreach (var item in published) item.Status = "Disabled";
    }

    private async Task<GameRuntimeToken?> LoadTokenAsync(string runtimeToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(runtimeToken)) return null;
        var hash = HashToken(runtimeToken.Trim());
        return await db.GameRuntimeTokens.AsNoTracking().FirstOrDefaultAsync(x => x.TokenHash == hash && x.RevokedAt == null && x.ExpiresAt > DateTimeOffset.UtcNow, ct);
    }

    private async Task<string> GetGuildPlanAsync(Guid guildId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        return await db.GuildSubscriptions.AsNoTracking().Include(x => x.SubscriptionPlan).Where(x => x.GuildId == guildId && x.Status == GuildSubscriptionStatus.Active && (x.ExpiresAt == null || x.ExpiresAt > now)).OrderByDescending(x => x.StartedAt).Select(x => x.SubscriptionPlan.Key).FirstOrDefaultAsync(ct) ?? "free";
    }

    private static bool IsPlanAllowed(string current, string required)
    {
        var rank = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["free"] = 0, ["basic"] = 1, ["pro"] = 2, ["premium"] = 3 };
        return rank.GetValueOrDefault(current, 0) >= rank.GetValueOrDefault(required, 0);
    }

    private static string? ValidateVersionRequest(CreateGameVersionRequest request)
    {
        if (!VersionPattern.IsMatch(request.Version.Trim())) return "رقم الإصدار غير صالح.";
        if (!VersionStatuses.Contains(request.Status)) return "حالة الإصدار غير صالحة.";
        return IsValidJson(request.ManifestJson) ? null : "Manifest JSON غير صالح.";
    }

    private static string? ValidateEventRequest(EmitGameEventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EventType) || request.EventType.Length > 120) return "نوع الحدث غير صالح.";
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 160) return "مفتاح منع التكرار مطلوب وبحد أقصى 160 حرف.";
        return IsValidJson(request.PayloadJson) ? null : "Payload JSON غير صالح.";
    }

    private static bool IsValidJson(string? json)
    {
        try { using var _ = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json); return true; } catch { return false; }
    }

    private static string NormalizeJson(string? json)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        return JsonSerializer.Serialize(doc.RootElement);
    }

    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return $"grt_{Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=')}";
    }

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    private static Guid StableGuid(string value) => new(MD5.HashData(Encoding.UTF8.GetBytes(value)));
    private static bool ValidSnowflake(string value) => ulong.TryParse(value, out _);
    private static string? TrimNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Clean(string? value, int max, string fallback) { var text = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim(); return text[..Math.Min(text.Length, max)]; }
    private static GameRuntimeContextDto MapRuntime(GameRuntimeToken x) => new() { GameKey = x.GameKey, GameVersionId = x.GameVersionId, GuildId = x.GuildId, GuildDiscordId = x.GuildDiscordId, ChannelDiscordId = x.ChannelDiscordId, UserDiscordId = x.UserDiscordId, ExpiresAt = x.ExpiresAt, Mode = x.Mode };
    private static GameSandboxAccessDto MapSandbox(GameSandboxAccess x) => new() { Id = x.Id, GameVersionId = x.GameVersionId, GuildDiscordId = x.GuildDiscordId, UserDiscordId = x.UserDiscordId, CreatedAt = x.CreatedAt };
    private static GameVersionDto MapVersion(GameVersion x) => new() { Id = x.Id, GameDefinitionId = x.GameDefinitionId, GameKey = x.GameDefinition?.Key ?? "", GameName = x.GameDefinition?.Name ?? "", Version = x.Version, Status = x.Status, FrontendUrl = x.FrontendUrl, BackendUrl = x.BackendUrl, ActivityRoute = x.ActivityRoute, ManifestJson = x.ManifestJson, Notes = x.Notes, CreatedAt = x.CreatedAt, PublishedAt = x.PublishedAt, SandboxAccess = x.SandboxAccess.Select(MapSandbox).ToList() };
    private static GameEventDto MapEvent(GameEvent x) => new() { Id = x.Id, GameKey = x.GameKey, GameVersionId = x.GameVersionId, EventType = x.EventType, Status = x.Status, PayloadJson = x.PayloadJson, IdempotencyKey = x.IdempotencyKey, CreatedAt = x.CreatedAt };
    private static GameBotPublishActionDto MapPublish(GameBotPublishAction x) => new() { Id = x.Id, GameEventId = x.GameEventId, ChannelDiscordId = x.ChannelDiscordId, Status = x.Status, MessageJson = x.MessageJson, CreatedAt = x.CreatedAt };
    private static PendingGameBotPublishActionDto MapPendingPublish(GameBotPublishAction x) => new() { Id = x.Id, GameEventId = x.GameEventId, DiscordGuildId = x.Guild.DiscordGuildId, ChannelDiscordId = x.ChannelDiscordId, Status = x.Status, MessageJson = x.MessageJson, CreatedAt = x.CreatedAt };
}
