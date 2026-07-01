using DiscordBot.Domain.Enums;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Services;

public class BotLogWriter
{
    private readonly BotApiClient _apiClient;
    private readonly ILogger<BotLogWriter> _logger;

    public BotLogWriter(BotApiClient apiClient, ILogger<BotLogWriter> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public Task WriteAsync(
        string discordGuildId,
        LogEventType type,
        string message,
        string? actorDiscordUserId = null,
        string? targetDiscordUserId = null,
        string? channelDiscordId = null,
        string? actorDisplayName = null,
        string? targetDisplayName = null,
        string? channelDisplayName = null,
        string? metadataJson = null,
        CancellationToken cancellationToken = default) =>
        _apiClient.CreateLogAsync(new CreateLogApiRequest
        {
            DiscordGuildId = discordGuildId,
            Type = type.ToString(),
            Message = message,
            ActorDiscordUserId = actorDiscordUserId,
            TargetDiscordUserId = targetDiscordUserId,
            ChannelDiscordId = channelDiscordId,
            ActorDisplayName = actorDisplayName,
            TargetDisplayName = targetDisplayName,
            ChannelDisplayName = channelDisplayName,
            MetadataJson = metadataJson
        }, cancellationToken);
}
