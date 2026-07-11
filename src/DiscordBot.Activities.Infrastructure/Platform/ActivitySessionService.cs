using DiscordBot.Activities.Application.Abstractions;
using DiscordBot.Activities.Application.Models;
using DiscordBot.Activities.Domain.Entities;
using DiscordBot.Activities.Infrastructure.Data;
using DiscordBot.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Activities.Infrastructure.Platform;

public class ActivitySessionService(ActivitiesDbContext db, IPlatformApiClient platform, ILogger<ActivitySessionService> logger) : IActivitySessionService
{
    public async Task<OperationResult<ActivitySessionDto>> CreateSessionAsync(CreateActivitySessionRequest request, TrustedDiscordUser user, CancellationToken cancellationToken = default)
    {
        if (!ValidSnowflake(request.DiscordGuildId) || !ValidSnowflake(request.DiscordChannelId) || string.IsNullOrWhiteSpace(request.GameKey))
            return OperationResult<ActivitySessionDto>.Fail("بيانات تشغيل اللعبة غير صالحة.");

        var access = await platform.ValidateGameAccessAsync(new ValidateGameAccessRequest
        {
            DiscordGuildId = request.DiscordGuildId,
            DiscordChannelId = request.DiscordChannelId,
            DiscordUserId = user.DiscordUserId,
            GameKey = request.GameKey
        }, cancellationToken);

        if (!access.Allowed) return OperationResult<ActivitySessionDto>.Fail(access.DenialReason ?? "لا تملك صلاحية تشغيل هذه اللعبة.", 403);

        var now = DateTimeOffset.UtcNow;
        var session = new ActivitySession
        {
            DiscordUserId = user.DiscordUserId,
            Username = user.Username,
            AvatarUrl = user.AvatarUrl,
            DiscordGuildId = request.DiscordGuildId,
            DiscordChannelId = request.DiscordChannelId,
            DiscordActivityInstanceId = string.IsNullOrWhiteSpace(request.DiscordActivityInstanceId) ? null : request.DiscordActivityInstanceId.Trim(),
            GameKey = access.GameKey,
            GameVersion = access.GameVersion,
            PlatformGameVersionId = access.PlatformGameVersionId,
            Mode = access.Mode,
            Status = "Active",
            ExpiresAtUtc = now.AddHours(2),
            LastSeenAtUtc = now
        };
        session.Players.Add(new ActivityPlayer { DiscordUserId = user.DiscordUserId, Username = user.Username, AvatarUrl = user.AvatarUrl, JoinedAtUtc = now, LastSeenAtUtc = now });
        db.ActivitySessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Activity session {ActivitySessionId} created for game {GameKey}, guild {DiscordGuildId}, user {DiscordUserId}.", session.Id, session.GameKey, session.DiscordGuildId, session.DiscordUserId);
        return OperationResult<ActivitySessionDto>.Ok(Map(session));
    }

    public async Task<bool> CanJoinGameSignalRGroupAsync(Guid activitySessionId, string discordUserId, CancellationToken cancellationToken = default)
    {
        return await db.ActivityPlayers.AsNoTracking()
            .AnyAsync(x => x.ActivitySessionId == activitySessionId && x.DiscordUserId == discordUserId, cancellationToken);
    }

    public async Task<bool> CanJoinRouletteGameSessionSignalRGroupAsync(Guid gameSessionId, string discordUserId, CancellationToken cancellationToken = default)
    {
        return await db.RouletteGameSessions.AsNoTracking()
            .Include(x => x.Players)
            .AnyAsync(x => x.GameSessionId == gameSessionId
                && x.Status != "Completed"
                && x.Status != "Cancelled"
                && x.Status != "Expired"
                && x.Players.Any(p => p.DiscordUserId == discordUserId), cancellationToken);
    }

    private static ActivitySessionDto Map(ActivitySession x) => new() { Id = x.Id, DiscordUserId = x.DiscordUserId, DiscordGuildId = x.DiscordGuildId, DiscordChannelId = x.DiscordChannelId, GameKey = x.GameKey, GameVersion = x.GameVersion, Mode = x.Mode, Status = x.Status, ExpiresAtUtc = x.ExpiresAtUtc };
    private static bool ValidSnowflake(string value) => ulong.TryParse(value, out _);
}
