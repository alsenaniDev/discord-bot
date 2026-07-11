using DiscordBot.Activities.Application.Models;
using DiscordBot.Shared;

namespace DiscordBot.Activities.Application.Abstractions;

public interface IRouletteRuntimeService
{
    Task<OperationResult<RouletteSessionDto>> CreateSessionAsync(CreateRouletteSessionRequest request, TrustedDiscordUser user, CancellationToken ct = default);
    Task<OperationResult<IReadOnlyList<RouletteSessionDto>>> GetOpenSessionsAsync(string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default);
    Task<OperationResult<MyActiveRouletteSessionDto>> GetMyActiveSessionAsync(string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default);
    Task<OperationResult<RouletteSessionDto>> GetSessionAsync(Guid gameSessionId, string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default);
    Task<OperationResult<RouletteSessionDto>> JoinSessionAsync(Guid gameSessionId, RouletteScopeRequest request, TrustedDiscordUser user, CancellationToken ct = default);
    Task<OperationResult<RouletteSessionDto>> LeaveSessionAsync(Guid gameSessionId, RouletteScopeRequest request, string userDiscordId, CancellationToken ct = default);
    Task<OperationResult<RouletteSessionDto>> StartSessionAsync(Guid gameSessionId, RouletteScopeRequest request, string userDiscordId, CancellationToken ct = default);
    Task<OperationResult<RouletteSpinResultDto>> SpinAsync(Guid gameSessionId, RouletteScopeRequest request, string userDiscordId, CancellationToken ct = default);
    Task<OperationResult<RouletteSessionDto>> ResolvePendingActionAsync(Guid gameSessionId, RouletteScopeRequest request, string userDiscordId, CancellationToken ct = default);
    Task<OperationResult<RouletteSessionDto>> ReconnectAsync(Guid gameSessionId, RouletteScopeRequest request, TrustedDiscordUser user, CancellationToken ct = default);
    Task<OperationResult<RouletteBetDto>> PlaceBetAsync(Guid gameSessionId, PlaceRouletteBetRequest request, string userDiscordId, CancellationToken ct = default);
}
