using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Services;

public class GameResultPublishService(BotApiClient api, ILogger<GameResultPublishService> logger)
{
    public async Task ProcessAsync(DiscordSocketClient client, CancellationToken ct)
    {
        var actions = await api.GetPendingGamePublishActionsAsync(ct);
        foreach (var action in actions)
        {
            try
            {
                if (!ulong.TryParse(action.DiscordGuildId, out var guildId) || !ulong.TryParse(action.ChannelDiscordId, out var channelId)) throw new InvalidOperationException("Invalid game publish guild or channel ID.");
                var guild = client.GetGuild(guildId) ?? throw new InvalidOperationException("Discord guild not found.");
                if (guild.GetChannel(channelId) is not IMessageChannel channel) throw new InvalidOperationException("Configured games channel is missing or is not a text channel.");
                await channel.SendMessageAsync(action.Content, allowedMentions: AllowedMentions.None);
                await api.AckGamePublishActionAsync(action.Id, new AckGamePublishActionApiRequest { Success = true }, ct);
                logger.LogInformation("Published game action {ActionId} for session {SessionId} to guild {GuildId}, channel {ChannelId}.", action.Id, action.GameSessionId, guildId, channelId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Game publish action {ActionId} for session {SessionId}, guild {GuildId}, channel {ChannelId} failed.", action.Id, action.GameSessionId, action.DiscordGuildId, action.ChannelDiscordId);
                await api.AckGamePublishActionAsync(action.Id, new AckGamePublishActionApiRequest { Success = false, ErrorMessage = ex.Message }, ct);
            }
        }
    }
}
