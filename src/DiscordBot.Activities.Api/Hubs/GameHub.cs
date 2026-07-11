using DiscordBot.Activities.Application.Abstractions;
using DiscordBot.Activities.Application.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DiscordBot.Activities.Api.Hubs;

[Authorize]
public class GameHub(IActivitySessionService sessions, ILogger<GameHub> logger) : Hub
{
    public async Task JoinActivitySession(Guid activitySessionId)
    {
        var discordUserId = Context.User?.FindFirst("discord_user_id")?.Value;
        if (string.IsNullOrWhiteSpace(discordUserId)) throw new HubException("غير مصرح.");
        if (!await sessions.CanJoinGameSignalRGroupAsync(activitySessionId, discordUserId, Context.ConnectionAborted))
            throw new HubException("لا تملك صلاحية الانضمام لهذه الجلسة.");
        var group = GroupName(activitySessionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, group, Context.ConnectionAborted);
        logger.LogInformation("SignalR connection {ConnectionId} joined {GroupName} for user {DiscordUserId}.", Context.ConnectionId, group, discordUserId);
    }

    public async Task JoinRouletteGameSession(Guid gameSessionId)
    {
        var discordUserId = Context.User?.FindFirst("discord_user_id")?.Value;
        if (string.IsNullOrWhiteSpace(discordUserId)) throw new HubException("غير مصرح.");
        if (!await sessions.CanJoinRouletteGameSessionSignalRGroupAsync(gameSessionId, discordUserId, Context.ConnectionAborted))
            throw new HubException("لا تملك صلاحية الانضمام لهذه الجلسة أو انتهت اللعبة.");
        var group = GameSessionGroupNames.Roulette(gameSessionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, group, Context.ConnectionAborted);
        logger.LogInformation("SignalR connection {ConnectionId} joined Roulette {GroupName} for user {DiscordUserId}.", Context.ConnectionId, group, discordUserId);
    }

    public async Task LeaveActivitySession(Guid activitySessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(activitySessionId), Context.ConnectionAborted);
    }

    private static string GroupName(Guid activitySessionId) => GameSessionGroupNames.Roulette(activitySessionId);
}
