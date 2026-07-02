using Discord.WebSocket;
using DiscordBot.Domain.Enums;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Services;

public class BotLogWriter
{
    private readonly BotApiClient _apiClient;
    private readonly DiscordSocketClient _client;
    private readonly DiscordLogDeliveryService _logDelivery;
    private readonly ILogger<BotLogWriter> _logger;

    public BotLogWriter(
        BotApiClient apiClient,
        DiscordSocketClient client,
        DiscordLogDeliveryService logDelivery,
        ILogger<BotLogWriter> logger)
    {
        _apiClient = apiClient;
        _client = client;
        _logDelivery = logDelivery;
        _logger = logger;
    }

    public async Task WriteAsync(
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
        CancellationToken cancellationToken = default)
    {
        var request = new CreateLogApiRequest
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
        };

        try
        {
            var persisted = await _apiClient.CreateLogAsync(request, cancellationToken);
            if (persisted)
            {
                await _logDelivery.TryDeliverAsync(_client, request, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write log for guild {GuildId}.", discordGuildId);
        }
    }
}
