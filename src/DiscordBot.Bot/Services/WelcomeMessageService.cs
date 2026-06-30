using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Api.Models;
using DiscordBot.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Services;

public class WelcomeMessageService
{
    private readonly EmbedBuilderService _embeds;
    private readonly BotLogWriter _logWriter;

    public WelcomeMessageService(EmbedBuilderService embeds, BotLogWriter logWriter)
    {
        _embeds = embeds;
        _logWriter = logWriter;
    }

    public string FormatMessage(string template, SocketGuildUser user, SocketGuild guild)
    {
        return template
            .Replace("{user}", user.Mention, StringComparison.OrdinalIgnoreCase)
            .Replace("{server}", guild.Name, StringComparison.OrdinalIgnoreCase);
    }

    public async Task SendWelcomeAsync(
        DiscordSocketClient client,
        SocketGuildUser user,
        GuildSettingsResponse settings,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!settings.WelcomeEnabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.WelcomeChannelId)
            || !ulong.TryParse(settings.WelcomeChannelId, out var channelId))
        {
            logger.LogWarning(
                "Welcome enabled for guild {GuildId} but WelcomeChannelId is missing or invalid.",
                user.Guild.Id);
            return;
        }

        var channel = client.GetChannel(channelId) as IMessageChannel;
        if (channel is null)
        {
            logger.LogWarning(
                "Welcome channel {ChannelId} not found in guild {GuildId}.",
                channelId,
                user.Guild.Id);
            return;
        }

        var message = FormatMessage(settings.WelcomeMessage, user, user.Guild);
        var allowedMentions = new AllowedMentions { AllowedTypes = AllowedMentionTypes.Users };
        await channel.SendMessageAsync(
            embed: _embeds.BuildWelcome(user, user.Guild, message),
            allowedMentions: allowedMentions);

        await _logWriter.WriteAsync(
            user.Guild.Id.ToString(),
            LogEventType.WelcomeSent,
            $"Welcome message sent to {user.Username}.",
            targetDiscordUserId: user.Id.ToString(),
            channelDiscordId: channelId.ToString(),
            cancellationToken: cancellationToken);
    }
}
