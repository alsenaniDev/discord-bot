using DiscordBot.Activities.Application.Models;
using DiscordBot.Shared;

namespace DiscordBot.Activities.Application.Abstractions;

public interface IActivitySessionService
{
    Task<OperationResult<ActivitySessionDto>> CreateSessionAsync(CreateActivitySessionRequest request, TrustedDiscordUser user, CancellationToken cancellationToken = default);
    Task<bool> CanJoinGameSignalRGroupAsync(Guid activitySessionId, string discordUserId, CancellationToken cancellationToken = default);
    Task<bool> CanJoinRouletteGameSessionSignalRGroupAsync(Guid gameSessionId, string discordUserId, CancellationToken cancellationToken = default);
}
