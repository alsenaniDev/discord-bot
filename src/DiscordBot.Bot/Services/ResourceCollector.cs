using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Api.Models;

namespace DiscordBot.Bot.Services;

/// <summary>
/// Reads channels and roles from a Discord guild for API sync.
/// </summary>
public static class ResourceCollector
{
    public static SyncResourcesApiRequest Collect(SocketGuild guild)
    {
        var channels = new List<SyncChannelApiItem>();

        foreach (var channel in guild.Channels.OrderBy(c => c.Position))
        {
            var type = channel.ChannelType switch
            {
                ChannelType.Text or ChannelType.News or ChannelType.Forum or ChannelType.Media => SyncChannelType.Text,
                ChannelType.Category => SyncChannelType.Category,
                ChannelType.Voice or ChannelType.Stage => SyncChannelType.Voice,
                _ => (SyncChannelType?)null
            };

            if (type is null)
            {
                continue;
            }

            channels.Add(new SyncChannelApiItem
            {
                DiscordChannelId = channel.Id.ToString(),
                Name = channel.Name,
                Type = type.Value,
                Position = channel.Position
            });
        }

        var roles = guild.Roles
            .Where(r => !r.IsEveryone)
            .OrderByDescending(r => r.Position)
            .Select(r => new SyncRoleApiItem
            {
                DiscordRoleId = r.Id.ToString(),
                Name = r.Name,
                Color = r.Color.RawValue == 0 ? null : (int?)r.Color.RawValue,
                Position = r.Position,
                IsManaged = r.IsManaged
            })
            .ToList();

        return new SyncResourcesApiRequest
        {
            Channels = channels,
            Roles = roles
        };
    }
}
