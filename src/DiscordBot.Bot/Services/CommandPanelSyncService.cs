using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Services;

public class CommandPanelSyncService
{
    private readonly BotApiClient _apiClient;
    private readonly EmbedBuilderService _embeds;
    private readonly ComponentBuilderService _components;
    private readonly ILogger<CommandPanelSyncService> _logger;

    public CommandPanelSyncService(
        BotApiClient apiClient,
        EmbedBuilderService embeds,
        ComponentBuilderService components,
        ILogger<CommandPanelSyncService> logger)
    {
        _apiClient = apiClient;
        _embeds = embeds;
        _components = components;
        _logger = logger;
    }

    public async Task ProcessPendingRefreshesAsync(
        DiscordSocketClient client,
        CancellationToken cancellationToken = default)
    {
        var pending = await _apiClient.GetPendingCommandPanelRefreshesAsync(cancellationToken);
        if (pending.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Processing {Count} command panel refresh request(s).", pending.Count);

        foreach (var item in pending)
        {
            try
            {
                await ProcessRefreshAsync(client, item, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to refresh command panel for guild {GuildId}.",
                    item.DiscordGuildId);
            }
        }
    }

    private async Task ProcessRefreshAsync(
        DiscordSocketClient client,
        CommandPanelRefreshApiResponse item,
        CancellationToken cancellationToken)
    {
        if (!item.Config.Enabled)
        {
            await _apiClient.AckCommandPanelAsync(
                item.DiscordGuildId,
                new AckCommandPanelApiRequest(),
                cancellationToken);
            return;
        }

        if (!ulong.TryParse(item.DiscordGuildId, out var guildId)
            || !ulong.TryParse(item.Config.ChannelId, out var channelId))
        {
            _logger.LogWarning(
                "Command panel refresh skipped for guild {GuildId}: invalid guild or channel id.",
                item.DiscordGuildId);
            return;
        }

        var guild = client.GetGuild(guildId);
        var channel = guild?.GetTextChannel(channelId);
        if (channel is null)
        {
            _logger.LogWarning(
                "Command panel channel {ChannelId} not found in guild {GuildId}.",
                channelId,
                guildId);
            return;
        }

        if (!string.IsNullOrWhiteSpace(item.Config.ImageUrl)
            && !IsValidPanelImageUrl(item.Config.ImageUrl))
        {
            _logger.LogWarning(
                "Command panel image URL is invalid for guild {GuildId}; embed will be posted without an image.",
                item.DiscordGuildId);
        }

        var embed = _embeds.BuildCommandPanel(
            item.Config.Title,
            item.Config.Description,
            item.Config.ImageUrl);
        var components = _components.BuildCommandPanelComponents(item.Config.Buttons);
        if (components.Components.Count == 0)
        {
            _logger.LogWarning(
                "Command panel for guild {GuildId} has no enabled buttons. Posting embed only.",
                item.DiscordGuildId);
        }

        IUserMessage? message = null;
        if (!string.IsNullOrWhiteSpace(item.Config.MessageId)
            && ulong.TryParse(item.Config.MessageId, out var messageId))
        {
            message = await channel.GetMessageAsync(messageId) as IUserMessage;
        }

        if (message is null)
        {
            message = components.Components.Count == 0
                ? await channel.SendMessageAsync(embed: embed)
                : await channel.SendMessageAsync(embed: embed, components: components);
        }
        else
        {
            await message.ModifyAsync(props =>
            {
                props.Embed = embed;
                props.Components = components.Components.Count == 0 ? null : components;
            });
        }

        await _apiClient.AckCommandPanelAsync(
            item.DiscordGuildId,
            new AckCommandPanelApiRequest { MessageId = message.Id.ToString() },
            cancellationToken);

        _logger.LogInformation(
            "Updated command panel in guild {GuildId}, channel {ChannelId}.",
            guildId,
            channelId);
    }

    private static bool IsValidPanelImageUrl(string url) =>
        Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
