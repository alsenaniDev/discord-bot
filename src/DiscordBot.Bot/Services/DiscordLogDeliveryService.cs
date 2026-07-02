using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;
using DiscordBot.Domain.Constants;
using DiscordBot.Domain.Enums;
using DiscordBot.Domain.Extensions;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Services;

public class DiscordLogDeliveryService
{
    private readonly BotApiClient _apiClient;
    private readonly ILogger<DiscordLogDeliveryService> _logger;

    public DiscordLogDeliveryService(BotApiClient apiClient, ILogger<DiscordLogDeliveryService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task TryDeliverAsync(
        DiscordSocketClient client,
        CreateLogApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<LogEventType>(request.Type, ignoreCase: true, out var eventType))
        {
            return;
        }

        if (!ShouldDeliverToDiscord(eventType))
        {
            return;
        }

        if (!LogEventTypeExtensions.IsCritical(eventType)
            && !await _apiClient.IsModuleEnabledAsync(request.DiscordGuildId, ModuleKeys.Logs, cancellationToken))
        {
            return;
        }

        var settings = await _apiClient.GetSettingsAsync(request.DiscordGuildId, cancellationToken);
        if (settings is null
            || !settings.LogsEnabled
            || string.IsNullOrWhiteSpace(settings.LogChannelId))
        {
            return;
        }

        if (!ulong.TryParse(request.DiscordGuildId, out var guildId)
            || !ulong.TryParse(settings.LogChannelId, out var channelId))
        {
            return;
        }

        var guild = client.GetGuild(guildId);
        var channel = guild?.GetTextChannel(channelId);
        if (channel is null)
        {
            _logger.LogWarning(
                "Log channel {ChannelId} not found in guild {GuildId}.",
                settings.LogChannelId,
                request.DiscordGuildId);
            return;
        }

        try
        {
            var embed = BuildLogEmbed(request, eventType);
            await channel.SendMessageAsync(embed: embed);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to deliver log to channel {ChannelId} in guild {GuildId}.",
                settings.LogChannelId,
                request.DiscordGuildId);
        }
    }

    private static bool ShouldDeliverToDiscord(LogEventType type) =>
        type is LogEventType.WelcomeSent
            or LogEventType.AutoRoleAssigned
            or LogEventType.TicketOpened
            or LogEventType.TicketClosed
            or LogEventType.TicketArchived
            or LogEventType.WarningCreated
            or LogEventType.MessagesCleared
            or LogEventType.MemberKicked
            or LogEventType.ReactionRoleAssigned
            or LogEventType.ReactionRoleRemoved;

    private static Embed BuildLogEmbed(CreateLogApiRequest request, LogEventType eventType)
    {
        var builder = new EmbedBuilder()
            .WithTitle(LogEventTypeExtensions.GetLabel(eventType))
            .WithDescription(request.Message)
            .WithColor(GetColor(eventType))
            .WithTimestamp(DateTimeOffset.UtcNow);

        if (!string.IsNullOrWhiteSpace(request.ActorDisplayName) || !string.IsNullOrWhiteSpace(request.ActorDiscordUserId))
        {
            builder.AddField(
                "Actor",
                FormatIdentity(request.ActorDisplayName, request.ActorDiscordUserId),
                inline: true);
        }

        if (!string.IsNullOrWhiteSpace(request.TargetDisplayName) || !string.IsNullOrWhiteSpace(request.TargetDiscordUserId))
        {
            builder.AddField(
                "Target",
                FormatIdentity(request.TargetDisplayName, request.TargetDiscordUserId),
                inline: true);
        }

        if (!string.IsNullOrWhiteSpace(request.ChannelDisplayName) || !string.IsNullOrWhiteSpace(request.ChannelDiscordId))
        {
            builder.AddField(
                "Channel",
                FormatIdentity(request.ChannelDisplayName, request.ChannelDiscordId),
                inline: true);
        }

        var footerParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.ActorDiscordUserId))
        {
            footerParts.Add($"Actor ID: {request.ActorDiscordUserId}");
        }

        if (!string.IsNullOrWhiteSpace(request.TargetDiscordUserId))
        {
            footerParts.Add($"Target ID: {request.TargetDiscordUserId}");
        }

        if (footerParts.Count > 0)
        {
            builder.WithFooter(string.Join(" · ", footerParts));
        }

        return builder.Build();
    }

    private static string FormatIdentity(string? name, string? id)
    {
        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(id))
        {
            return $"{name}\n`{id}`";
        }

        return name ?? (string.IsNullOrWhiteSpace(id) ? "—" : $"`{id}`");
    }

    private static Color GetColor(LogEventType type) =>
        type switch
        {
            LogEventType.WarningCreated => Color.Gold,
            LogEventType.MemberKicked or LogEventType.MessagesCleared => Color.Orange,
            LogEventType.TicketOpened or LogEventType.TicketClosed or LogEventType.TicketArchived => Color.Blue,
            LogEventType.ReactionRoleAssigned or LogEventType.ReactionRoleRemoved => Color.Purple,
            _ => Color.DarkGrey
        };
}
