using Discord.WebSocket;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;
using DiscordBot.Domain.Constants;
using DiscordBot.Domain.Enums;
using DiscordBot.Domain.Helpers;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Services;

public class TicketOutboundMessageService
{
    private readonly BotApiClient _apiClient;
    private readonly ILogger<TicketOutboundMessageService> _logger;

    public TicketOutboundMessageService(BotApiClient apiClient, ILogger<TicketOutboundMessageService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task ProcessPendingMessagesAsync(
        DiscordSocketClient client,
        CancellationToken cancellationToken = default)
    {
        var pending = await _apiClient.GetPendingTicketMessagesAsync(cancellationToken);
        foreach (var item in pending)
        {
            try
            {
                await ProcessMessageAsync(client, item, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to deliver outbound ticket message {MessageId} for ticket {TicketId}.",
                    item.Id,
                    item.TicketId);
            }
        }
    }

    private async Task ProcessMessageAsync(
        DiscordSocketClient client,
        PendingTicketMessageApiResponse item,
        CancellationToken cancellationToken)
    {
        if (!ulong.TryParse(item.DiscordGuildId, out var guildId)
            || !ulong.TryParse(item.ChannelDiscordId, out var channelId))
        {
            await _apiClient.AckTicketMessageAsync(item.Id, cancellationToken);
            return;
        }

        var guild = client.GetGuild(guildId);
        var channel = guild?.GetTextChannel(channelId);
        if (channel is null)
        {
            await _apiClient.AckTicketMessageAsync(item.Id, cancellationToken);
            return;
        }

        var staffName = string.IsNullOrWhiteSpace(item.SenderDisplayName)
            ? "Staff"
            : item.SenderDisplayName;

        var prefix = MessageTemplateFormatter.Format(
            string.IsNullOrWhiteSpace(item.StaffReplyPrefix)
                ? TicketMessageDefaults.StaffReplyPrefix
                : item.StaffReplyPrefix,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["staff"] = staffName
            });

        var content = string.IsNullOrWhiteSpace(prefix)
            ? item.Content
            : $"{prefix}\n{item.Content}";

        await channel.SendMessageAsync(content);
        await _apiClient.AckTicketMessageAsync(item.Id, cancellationToken);
    }
}
