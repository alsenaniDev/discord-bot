using System.Text.Json;
using System.Text.Json.Nodes;
using DiscordBot.Domain.Constants;
using DiscordBot.Domain.Entities;
using DiscordBot.Domain.Enums;
using DiscordBot.Domain.Extensions;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface ILogService
{
    Task<LogEntryDto?> CreateLogAsync(
        CreateLogRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LogEntryDto>> GetLogsAsync(
        Guid guildId,
        string ownerDiscordUserId,
        LogListFilter filter,
        CancellationToken cancellationToken = default);
}

public class LogService : ILogService
{
    private readonly AppDbContext _dbContext;
    private readonly IGuildAccessService _guildAccessService;

    public LogService(AppDbContext dbContext, IGuildAccessService guildAccessService)
    {
        _dbContext = dbContext;
        _guildAccessService = guildAccessService;
    }

    public async Task<LogEntryDto?> CreateLogAsync(
        CreateLogRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return null;
        }

        var guild = await ResolveGuildAsync(request, cancellationToken);
        if (guild is null)
        {
            return null;
        }

        if (!await ShouldWriteAsync(guild.Id, request.Type, cancellationToken))
        {
            return null;
        }

        request.MetadataJson = await MergeDisplayNamesIntoMetadataAsync(
            request.MetadataJson,
            request.ActorDiscordUserId,
            request.TargetDiscordUserId,
            request.ActorDisplayName,
            request.TargetDisplayName,
            request.ChannelDisplayName,
            cancellationToken);

        var entry = new LogEntry
        {
            GuildId = guild.Id,
            Type = request.Type,
            Message = request.Message.Trim(),
            ActorDiscordUserId = request.ActorDiscordUserId,
            TargetDiscordUserId = request.TargetDiscordUserId,
            ChannelDiscordId = request.ChannelDiscordId,
            MetadataJson = request.MetadataJson
        };

        _dbContext.LogEntries.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await EnrichLogsAsync(guild.Id, [entry], cancellationToken)).FirstOrDefault();
    }

    public async Task<IReadOnlyList<LogEntryDto>> GetLogsAsync(
        Guid guildId,
        string ownerDiscordUserId,
        LogListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var hasAccess = await _guildAccessService.CanAccessModerationPagesAsync(
            guildId,
            ownerDiscordUserId,
            cancellationToken);

        if (!hasAccess)
        {
            return [];
        }

        var query = _dbContext.LogEntries
            .AsNoTracking()
            .Where(l => l.GuildId == guildId);

        if (filter.Type.HasValue)
        {
            query = query.Where(l => l.Type == filter.Type.Value);
        }

        if (filter.From.HasValue)
        {
            query = query.Where(l => l.CreatedAt >= filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            query = query.Where(l => l.CreatedAt <= filter.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(l =>
                l.Message.Contains(term)
                || (l.ActorDiscordUserId != null && l.ActorDiscordUserId.Contains(term))
                || (l.TargetDiscordUserId != null && l.TargetDiscordUserId.Contains(term))
                || (l.ChannelDiscordId != null && l.ChannelDiscordId.Contains(term))
                || (l.MetadataJson != null && l.MetadataJson.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(filter.UserId))
        {
            var userId = filter.UserId.Trim();
            query = query.Where(l =>
                l.ActorDiscordUserId == userId
                || l.TargetDiscordUserId == userId
                || (l.MetadataJson != null && l.MetadataJson.Contains(userId)));
        }

        var entries = await query
            .OrderByDescending(l => l.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return await EnrichLogsAsync(guildId, entries, cancellationToken);
    }

    private async Task<IReadOnlyList<LogEntryDto>> EnrichLogsAsync(
        Guid guildId,
        IReadOnlyList<LogEntry> entries,
        CancellationToken cancellationToken)
    {
        if (entries.Count == 0)
        {
            return [];
        }

        var channelIds = entries
            .Select(e => e.ChannelDiscordId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        var userIds = entries
            .SelectMany(e => new[] { e.ActorDiscordUserId, e.TargetDiscordUserId })
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        var channels = channelIds.Count == 0
            ? new Dictionary<string, string>()
            : await _dbContext.DiscordChannels
                .AsNoTracking()
                .Where(c => c.GuildId == guildId && channelIds.Contains(c.DiscordChannelId))
                .ToDictionaryAsync(c => c.DiscordChannelId, c => c.Name, cancellationToken);

        var users = userIds.Count == 0
            ? new Dictionary<string, string>()
            : await _dbContext.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.DiscordUserId))
                .ToDictionaryAsync(
                    u => u.DiscordUserId,
                    u => u.GlobalName ?? u.Username,
                    cancellationToken);

        var guildMembers = userIds.Count == 0
            ? new Dictionary<string, string>()
            : await _dbContext.DiscordGuildMembers
                .AsNoTracking()
                .Where(m => m.GuildId == guildId && userIds.Contains(m.DiscordUserId))
                .ToDictionaryAsync(
                    m => m.DiscordUserId,
                    m => m.Nickname ?? m.GlobalName ?? m.Username,
                    cancellationToken);

        foreach (var (memberId, displayName) in guildMembers)
        {
            users[memberId] = displayName;
        }

        return entries
            .Select(entry => MapEnriched(entry, channels, users))
            .ToList();
    }

    private static LogEntryDto MapEnriched(
        LogEntry entry,
        IReadOnlyDictionary<string, string> channels,
        IReadOnlyDictionary<string, string> users)
    {
        var metadataNames = ParseMetadataNames(entry.MetadataJson);

        var actorDisplayName = metadataNames.ActorName
            ?? TryGetUserDisplayName(users, entry.ActorDiscordUserId);

        var targetDisplayName = metadataNames.TargetName
            ?? TryGetUserDisplayName(users, entry.TargetDiscordUserId)
            ?? TryExtractTargetNameFromMessage(entry.Type, entry.Message);

        var channelName = metadataNames.ChannelName
            ?? TryGetChannelDisplayName(channels, entry.ChannelDiscordId);

        return new LogEntryDto
        {
            Id = entry.Id,
            Type = entry.Type,
            TypeLabel = LogEventTypeExtensions.GetLabel(entry.Type),
            Message = entry.Message,
            ActorDiscordUserId = entry.ActorDiscordUserId,
            TargetDiscordUserId = entry.TargetDiscordUserId,
            ChannelDiscordId = entry.ChannelDiscordId,
            ActorDisplayName = actorDisplayName,
            TargetDisplayName = targetDisplayName,
            ChannelName = channelName,
            MetadataJson = entry.MetadataJson,
            CreatedAt = entry.CreatedAt
        };
    }

    private async Task<string?> MergeDisplayNamesIntoMetadataAsync(
        string? metadataJson,
        string? actorDiscordUserId,
        string? targetDiscordUserId,
        string? actorDisplayName,
        string? targetDisplayName,
        string? channelDisplayName,
        CancellationToken cancellationToken)
    {
        var metadata = ParseMetadataDictionary(metadataJson);
        var changed = false;

        if (string.IsNullOrWhiteSpace(GetMetadataString(metadata, "actorName")))
        {
            var resolvedActorName = actorDisplayName?.Trim()
                ?? await ResolveUserDisplayNameAsync(actorDiscordUserId, cancellationToken);

            if (!string.IsNullOrWhiteSpace(resolvedActorName))
            {
                changed = true;
            }
        }

        if (string.IsNullOrWhiteSpace(GetMetadataString(metadata, "targetName"))
            && !string.IsNullOrWhiteSpace(targetDisplayName))
        {
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(GetMetadataString(metadata, "channelName"))
            && !string.IsNullOrWhiteSpace(channelDisplayName))
        {
            changed = true;
        }

        if (!changed)
        {
            return metadataJson;
        }

        var node = string.IsNullOrWhiteSpace(metadataJson)
            ? new JsonObject()
            : JsonNode.Parse(metadataJson)?.AsObject() ?? new JsonObject();

        if (string.IsNullOrWhiteSpace(GetMetadataString(metadata, "actorName")))
        {
            var resolvedActorName = actorDisplayName?.Trim()
                ?? await ResolveUserDisplayNameAsync(actorDiscordUserId, cancellationToken);

            if (!string.IsNullOrWhiteSpace(resolvedActorName))
            {
                node["actorName"] = resolvedActorName;
            }
        }

        if (string.IsNullOrWhiteSpace(GetMetadataString(metadata, "targetName"))
            && !string.IsNullOrWhiteSpace(targetDisplayName))
        {
            node["targetName"] = targetDisplayName.Trim();
        }

        if (string.IsNullOrWhiteSpace(GetMetadataString(metadata, "channelName"))
            && !string.IsNullOrWhiteSpace(channelDisplayName))
        {
            node["channelName"] = channelDisplayName.Trim();
        }

        return node.Count == 0 ? metadataJson : node.ToJsonString();
    }

    private async Task<string?> ResolveUserDisplayNameAsync(
        string? discordUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return null;
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.DiscordUserId == discordUserId, cancellationToken);

        return user is null ? null : user.GlobalName ?? user.Username;
    }

    private static string? TryGetUserDisplayName(
        IReadOnlyDictionary<string, string> users,
        string? discordUserId)
    {
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return null;
        }

        return users.TryGetValue(discordUserId, out var name) ? name : null;
    }

    private static string? TryGetChannelDisplayName(
        IReadOnlyDictionary<string, string> channels,
        string? channelDiscordId)
    {
        if (string.IsNullOrWhiteSpace(channelDiscordId))
        {
            return null;
        }

        return channels.TryGetValue(channelDiscordId, out var name) ? FormatChannelName(name) : null;
    }

    private static string FormatChannelName(string name) =>
        name.StartsWith('#') ? name : $"#{name}";

    private static (string? ActorName, string? TargetName, string? ChannelName) ParseMetadataNames(
        string? metadataJson)
    {
        var metadata = ParseMetadataDictionary(metadataJson);

        return (
            GetMetadataString(metadata, "actorName"),
            GetMetadataString(metadata, "targetName"),
            GetMetadataString(metadata, "channelName"));
    }

    private static Dictionary<string, JsonElement> ParseMetadataDictionary(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(metadataJson);
            return parsed is null
                ? new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, JsonElement>(parsed, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string? GetMetadataString(
        IReadOnlyDictionary<string, JsonElement> metadata,
        string key)
    {
        if (!metadata.TryGetValue(key, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString()?.Trim(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static string? TryExtractTargetNameFromMessage(LogEventType type, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        return type switch
        {
            LogEventType.MemberJoined when message.EndsWith(" joined the server.", StringComparison.Ordinal)
                => message[..^" joined the server.".Length].Trim(),
            LogEventType.WelcomeSent when message.StartsWith("Welcome message sent to ", StringComparison.Ordinal)
                                           && message.EndsWith('.')
                => message["Welcome message sent to ".Length..^1].Trim(),
            LogEventType.AutoRoleAssigned or LogEventType.ReactionRoleAssigned
                when TryExtractSegmentAfter(message, " to ", out var assignedName)
                => assignedName,
            LogEventType.ReactionRoleRemoved
                when TryExtractSegmentAfter(message, " from ", out var removedName)
                => removedName,
            _ => null
        };
    }

    private static bool TryExtractSegmentAfter(string message, string marker, out string? value)
    {
        value = null;

        if (!message.EndsWith('.') || !message.Contains(marker, StringComparison.Ordinal))
        {
            return false;
        }

        var startIndex = message.LastIndexOf(marker, StringComparison.Ordinal) + marker.Length;
        value = message[startIndex..^1].Trim();
        return !string.IsNullOrWhiteSpace(value);
    }

    private async Task<Guild?> ResolveGuildAsync(
        CreateLogRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DiscordGuildId))
        {
            return null;
        }

        return await _dbContext.Guilds
            .AsNoTracking()
            .FirstOrDefaultAsync(
                g => g.DiscordGuildId == request.DiscordGuildId && g.IsActive,
                cancellationToken);
    }

    private async Task<bool> ShouldWriteAsync(
        Guid guildId,
        LogEventType type,
        CancellationToken cancellationToken)
    {
        if (LogEventTypeExtensions.IsCritical(type))
        {
            return true;
        }

        return await _dbContext.GuildModules
            .AsNoTracking()
            .AnyAsync(
                gm => gm.GuildId == guildId
                      && gm.Module.Key == ModuleKeys.Logs
                      && gm.IsEnabled,
                cancellationToken);
    }

    internal static string BuildMetadataJson(object metadata) =>
        JsonSerializer.Serialize(metadata);
}
