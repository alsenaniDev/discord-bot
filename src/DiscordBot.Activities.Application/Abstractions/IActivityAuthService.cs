using DiscordBot.Activities.Application.Models;
using DiscordBot.Shared;

namespace DiscordBot.Activities.Application.Abstractions;

public interface IActivityAuthService
{
    Task<OperationResult<ActivityAuthResponse>> ExchangeDiscordCodeAsync(ExchangeDiscordCodeRequest request, CancellationToken cancellationToken = default);
}
