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
            _logger.LogInformation(
                "Processing panel {PanelId}: guild {GuildId}, channel {ChannelId}, existing message {MessageId}.",
                item.PanelId, item.DiscordGuildId, item.ChannelDiscordId, item.MessageDiscordId ?? "none");
            try
            {
                await ProcessRefreshAsync(client, item, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to publish panel {PanelId} in guild {GuildId}, channel {ChannelId}.",
                    item.PanelId,
                    item.DiscordGuildId,
                    item.ChannelDiscordId);
                await _apiClient.AckCommandPanelAsync(item.PanelId,
                    new AckCommandPanelApiRequest { Success = false, FailureReason = ex.Message }, cancellationToken);
            }
        }
    }

    private async Task ProcessRefreshAsync(
        DiscordSocketClient client,
        CommandPanelRefreshApiResponse item,
        CancellationToken cancellationToken)
    {
        if (!ulong.TryParse(item.DiscordGuildId, out var guildId)
            || !ulong.TryParse(item.ChannelDiscordId, out var channelId))
        {
            _logger.LogWarning(
                "Command panel refresh skipped for guild {GuildId}: invalid guild or channel id.",
                item.DiscordGuildId);
            throw new InvalidOperationException("Invalid Discord guild or channel ID.");
        }

        if (item.PanelId == Guid.Empty)
            throw new InvalidOperationException("The pending panel response did not contain a valid panel ID.");

        var guild = client.GetGuild(guildId)
            ?? throw new InvalidOperationException("The bot could not find the configured Discord guild.");
        var resolvedChannel = guild.GetChannel(channelId);
        if (resolvedChannel is not SocketTextChannel channel)
        {
            _logger.LogWarning(
                "Panel {PanelId} channel {ChannelId} was not found or is not a text channel in guild {GuildId}.",
                item.PanelId,
                channelId,
                guildId);
            throw new InvalidOperationException("Configured Discord channel was not found or is not a text channel.");
        }

        var permissions = guild.CurrentUser.GetPermissions(channel);
        if (!permissions.ViewChannel || !permissions.SendMessages || !permissions.EmbedLinks)
        {
            throw new InvalidOperationException("The bot needs View Channel, Send Messages, and Embed Links permissions in the configured channel.");
        }

        var imageUrl = item.ImageUrl;
        if (!string.IsNullOrWhiteSpace(imageUrl)
            && !IsValidPanelImageUrl(imageUrl))
        {
            _logger.LogWarning(
                "Command panel image URL is invalid for guild {GuildId}; embed will be posted without an image.",
                item.DiscordGuildId);
            imageUrl = null;
        }

        var embed = _embeds.BuildCommandPanel(
            item.Title,
            item.Description,
            imageUrl);
        var components = _components.BuildCommandPanelComponents(item.PanelId, item.Buttons);
        if (components.Components.Count == 0)
        {
            _logger.LogWarning(
                "Command panel for guild {GuildId} has no enabled buttons. Posting embed only.",
                item.DiscordGuildId);
        }

        IUserMessage? message = null;
        if (!string.IsNullOrWhiteSpace(item.MessageDiscordId)
            && ulong.TryParse(item.MessageDiscordId, out var messageId))
        {
            try
            {
                message = await channel.GetMessageAsync(messageId) as IUserMessage;
            }
            catch (Discord.Net.HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("Panel {PanelId} message {MessageId} no longer exists; creating a replacement.", item.PanelId, messageId);
            }
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
            item.PanelId,
            new AckCommandPanelApiRequest { MessageDiscordId = message.Id.ToString(), Success = true },
            cancellationToken);

        _logger.LogInformation(
            "Published panel {PanelId} in guild {GuildId}, channel {ChannelId}, message {MessageId}.",
            item.PanelId,
            guildId,
            channelId,
            message.Id);
    }

    private static bool IsValidPanelImageUrl(string url) =>
        Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
