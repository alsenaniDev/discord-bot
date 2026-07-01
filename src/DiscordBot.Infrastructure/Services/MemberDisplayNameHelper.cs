using System.Text.Json;
using System.Text.Json.Nodes;
using DiscordBot.Domain.Entities;
using DiscordBot.Domain.Enums;
using DiscordBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

internal static class MemberDisplayNameHelper
{
    public static async Task<IReadOnlyDictionary<string, string>> ResolveMemberNamesAsync(
        AppDbContext dbContext,
        Guid guildId,
        IEnumerable<string?> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        var names = new Dictionary<string, string>();

        var guildMembers = await dbContext.DiscordGuildMembers
            .AsNoTracking()
            .Where(m => m.GuildId == guildId && ids.Contains(m.DiscordUserId))
            .ToListAsync(cancellationToken);

        foreach (var member in guildMembers)
        {
            names[member.DiscordUserId] = FormatMemberName(member);
        }

        var missingIds = ids.Where(id => !names.ContainsKey(id)).ToList();
        if (missingIds.Count > 0)
        {
            var users = await dbContext.Users
                .AsNoTracking()
                .Where(u => missingIds.Contains(u.DiscordUserId))
                .ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                names[user.DiscordUserId] = user.GlobalName ?? user.Username;
            }
        }

        missingIds = ids.Where(id => !names.ContainsKey(id)).ToList();
        if (missingIds.Count > 0)
        {
            await MergeNamesFromLogsAsync(dbContext, guildId, missingIds, names, cancellationToken);
        }

        return names;
    }

    public static async Task<IReadOnlyDictionary<string, string>> ResolveChannelNamesAsync(
        AppDbContext dbContext,
        Guid guildId,
        IEnumerable<string?> channelIds,
        CancellationToken cancellationToken = default)
    {
        var ids = channelIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        var names = await dbContext.DiscordChannels
            .AsNoTracking()
            .Where(c => c.GuildId == guildId && ids.Contains(c.DiscordChannelId))
            .ToDictionaryAsync(c => c.DiscordChannelId, c => c.Name, cancellationToken);

        var missingIds = ids.Where(id => !names.ContainsKey(id)).ToList();
        if (missingIds.Count == 0)
        {
            return names;
        }

        var logs = await dbContext.LogEntries
            .AsNoTracking()
            .Where(l => l.GuildId == guildId
                        && l.ChannelDiscordId != null
                        && missingIds.Contains(l.ChannelDiscordId))
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new { l.ChannelDiscordId, l.MetadataJson })
            .ToListAsync(cancellationToken);

        foreach (var log in logs)
        {
            if (log.ChannelDiscordId is null || names.ContainsKey(log.ChannelDiscordId))
            {
                continue;
            }

            var channelName = ParseMetadataString(log.MetadataJson, "channelName");
            if (!string.IsNullOrWhiteSpace(channelName))
            {
                names[log.ChannelDiscordId] = channelName;
            }
        }

        return names;
    }

    public static async Task EnsureMemberKnownAsync(
        AppDbContext dbContext,
        Guid guildId,
        string discordUserId,
        string? displayName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(discordUserId) || string.IsNullOrWhiteSpace(displayName))
        {
            return;
        }

        var trimmedName = displayName.Trim();
        var existing = await dbContext.DiscordGuildMembers
            .FirstOrDefaultAsync(
                m => m.GuildId == guildId && m.DiscordUserId == discordUserId,
                cancellationToken);

        if (existing is null)
        {
            dbContext.DiscordGuildMembers.Add(new DiscordGuildMember
            {
                GuildId = guildId,
                DiscordUserId = discordUserId,
                Username = trimmedName,
                GlobalName = trimmedName
            });
            return;
        }

        if (string.IsNullOrWhiteSpace(existing.GlobalName)
            && string.IsNullOrWhiteSpace(existing.Nickname))
        {
            existing.GlobalName = trimmedName;
        }

        if (string.IsNullOrWhiteSpace(existing.Username))
        {
            existing.Username = trimmedName;
        }
    }

    public static async Task EnsureChannelKnownAsync(
        AppDbContext dbContext,
        Guid guildId,
        string channelDiscordId,
        string? channelName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(channelDiscordId) || string.IsNullOrWhiteSpace(channelName))
        {
            return;
        }

        var exists = await dbContext.DiscordChannels
            .AnyAsync(
                c => c.GuildId == guildId && c.DiscordChannelId == channelDiscordId,
                cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.DiscordChannels.Add(new DiscordChannel
        {
            GuildId = guildId,
            DiscordChannelId = channelDiscordId,
            Name = channelName.Trim(),
            Type = DiscordChannelType.Text,
            Position = 0
        });
    }

    private static async Task MergeNamesFromLogsAsync(
        AppDbContext dbContext,
        Guid guildId,
        IReadOnlyList<string> missingIds,
        Dictionary<string, string> names,
        CancellationToken cancellationToken)
    {
        var logs = await dbContext.LogEntries
            .AsNoTracking()
            .Where(l => l.GuildId == guildId)
            .Where(l =>
                (l.ActorDiscordUserId != null && missingIds.Contains(l.ActorDiscordUserId))
                || (l.TargetDiscordUserId != null && missingIds.Contains(l.TargetDiscordUserId)))
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new
            {
                l.ActorDiscordUserId,
                l.TargetDiscordUserId,
                l.MetadataJson
            })
            .ToListAsync(cancellationToken);

        foreach (var log in logs)
        {
            if (!string.IsNullOrWhiteSpace(log.ActorDiscordUserId)
                && missingIds.Contains(log.ActorDiscordUserId)
                && !names.ContainsKey(log.ActorDiscordUserId))
            {
                var actorName = ParseMetadataString(log.MetadataJson, "actorName");
                if (!string.IsNullOrWhiteSpace(actorName))
                {
                    names[log.ActorDiscordUserId] = actorName;
                }
            }

            if (!string.IsNullOrWhiteSpace(log.TargetDiscordUserId)
                && missingIds.Contains(log.TargetDiscordUserId)
                && !names.ContainsKey(log.TargetDiscordUserId))
            {
                var targetName = ParseMetadataString(log.MetadataJson, "targetName");
                if (!string.IsNullOrWhiteSpace(targetName))
                {
                    names[log.TargetDiscordUserId] = targetName;
                }
            }
        }
    }

    private static string FormatMemberName(DiscordGuildMember member) =>
        member.Nickname ?? member.GlobalName ?? member.Username;

    private static string? ParseMetadataString(string? metadataJson, string key)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            var node = JsonNode.Parse(metadataJson)?.AsObject();
            return node?[key]?.GetValue<string>()?.Trim();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
