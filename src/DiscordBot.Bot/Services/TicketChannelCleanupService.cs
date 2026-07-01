using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Services;

public class TicketChannelCleanupService
{
    private readonly BotApiClient _apiClient;
    private readonly EmbedBuilderService _embeds;
    private readonly ILogger<TicketChannelCleanupService> _logger;

    public TicketChannelCleanupService(
        BotApiClient apiClient,
        EmbedBuilderService embeds,
        ILogger<TicketChannelCleanupService> logger)
    {
        _apiClient = apiClient;
        _embeds = embeds;
        _logger = logger;
    }

    public async Task ProcessPendingCleanupsAsync(
        DiscordSocketClient client,
        CancellationToken cancellationToken = default)
    {
        var pending = await _apiClient.GetPendingTicketCleanupsAsync(cancellationToken);
        foreach (var item in pending)
        {
            try
            {
                await ProcessCleanupAsync(client, item, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to clean up ticket channel {ChannelId} for ticket {TicketId}.",
                    item.ChannelDiscordId,
                    item.TicketId);
            }
        }
    }

    private async Task ProcessCleanupAsync(
        DiscordSocketClient client,
        TicketCleanupApiResponse item,
        CancellationToken cancellationToken)
    {
        if (!ulong.TryParse(item.DiscordGuildId, out var guildId)
            || !ulong.TryParse(item.ChannelDiscordId, out var channelId))
        {
            await _apiClient.AckTicketCleanupAsync(item.TicketId, cancellationToken);
            return;
        }

        var guild = client.GetGuild(guildId);
        var channel = guild?.GetTextChannel(channelId);
        if (channel is not null)
        {
            try
            {
                await channel.SendMessageAsync(
                    embed: _embeds.BuildTicketClosedFromDashboard(
                        item.TicketNumber,
                        item.TicketClosedFromDashboardMessage));
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
                await channel.DeleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Could not delete ticket channel {ChannelId} in guild {GuildId}.",
                    channelId,
                    guildId);
            }
        }

        await _apiClient.AckTicketCleanupAsync(item.TicketId, cancellationToken);
    }
}
