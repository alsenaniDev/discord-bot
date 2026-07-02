using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Services;

/// <summary>
/// Captures Discord ticket channel messages as MessageSent Timeline Events (D-001 §8, BR-T01).
/// </summary>
public class TicketTimelineMessageService
{
    private readonly BotApiClient _apiClient;
    private readonly ILogger<TicketTimelineMessageService> _logger;

    public TicketTimelineMessageService(BotApiClient apiClient, ILogger<TicketTimelineMessageService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task HandleMessageAsync(SocketMessage message, CancellationToken cancellationToken = default)
    {
        if (message.Author.IsBot)
        {
            return;
        }

        if (message.Channel is not SocketTextChannel channel || channel.Guild is null)
        {
            return;
        }

        var content = message.Content?.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        var ticket = await _apiClient.GetTicketByChannelAsync(channel.Id.ToString(), cancellationToken);
        if (ticket is null)
        {
            return;
        }

        var author = message.Author as SocketGuildUser;
        var displayName = author?.GlobalName ?? author?.DisplayName ?? message.Author.Username;

        await _apiClient.RecordTicketMessageSentAsync(
            new RecordTicketMessageSentApiRequest
            {
                ChannelDiscordId = channel.Id.ToString(),
                DiscordMessageId = message.Id.ToString(),
                AuthorDiscordUserId = message.Author.Id.ToString(),
                AuthorDisplayName = displayName,
                Content = content,
                OccurredAt = message.Timestamp.UtcDateTime == default
                    ? DateTimeOffset.UtcNow
                    : message.Timestamp
            },
            cancellationToken);
    }
}
