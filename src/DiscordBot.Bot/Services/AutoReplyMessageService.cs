using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;
using DiscordBot.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Services;

public class AutoReplyMessageService
{
    private readonly BotApiClient _apiClient;
    private readonly ILogger<AutoReplyMessageService> _logger;

    public AutoReplyMessageService(BotApiClient apiClient, ILogger<AutoReplyMessageService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task HandleMessageAsync(SocketMessage message)
    {
        if (message.Author.IsBot || message.Author.IsWebhook)
        {
            return;
        }

        if (message.Channel is not ITextChannel textChannel)
        {
            return;
        }

        var guildChannel = message.Channel as SocketGuildChannel;
        if (guildChannel?.Guild is null)
        {
            return;
        }

        var guild = guildChannel.Guild;
        var rules = await _apiClient.GetAutoReplyRulesAsync(guild.Id.ToString());
        if (rules.Count == 0)
        {
            return;
        }

        var content = message.Content?.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogDebug(
                "Skipping auto-reply in guild {GuildId}: message content is empty. " +
                "Enable Message Content Intent in the Discord Developer Portal and restart the bot.",
                guild.Id);
            return;
        }

        var isTicketChannel = false;
        if (rules.Any(r => r.Scope == AutoReplyScope.TicketChannelsOnly))
        {
            var ticket = await _apiClient.GetTicketByChannelAsync(guildChannel.Id.ToString());
            isTicketChannel = ticket is not null;
        }

        foreach (var rule in rules.OrderBy(r => r.Priority).ThenBy(r => r.Id))
        {
            if (rule.Scope == AutoReplyScope.TicketChannelsOnly && !isTicketChannel)
            {
                continue;
            }

            if (!Matches(content, rule))
            {
                continue;
            }

            try
            {
                await textChannel.SendMessageAsync(rule.Response);
                _logger.LogInformation(
                    "Sent auto-reply in guild {GuildId} channel {ChannelId} for trigger \"{Trigger}\".",
                    guild.Id,
                    guildChannel.Id,
                    rule.Trigger);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Could not send auto-reply in guild {GuildId} channel {ChannelId}.",
                    guild.Id,
                    guildChannel.Id);
            }

            return;
        }
    }

    private static bool Matches(string messageContent, AutoReplyRuleApiResponse rule)
    {
        if (string.IsNullOrWhiteSpace(messageContent) || string.IsNullOrWhiteSpace(rule.Trigger))
        {
            return false;
        }

        var trigger = rule.Trigger.Trim();

        return rule.MatchMode switch
        {
            AutoReplyMatchMode.Exact => string.Equals(
                messageContent,
                trigger,
                StringComparison.OrdinalIgnoreCase),
            _ => messageContent.Contains(trigger, StringComparison.OrdinalIgnoreCase)
        };
    }
}
