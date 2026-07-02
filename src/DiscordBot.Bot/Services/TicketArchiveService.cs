using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;
using DiscordBot.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Services;

public class TicketArchiveService
{
    private const int MaxPreviewMessages = 8;
    private const int MaxPreviewLength = 1500;

    private readonly BotApiClient _apiClient;
    private readonly EmbedBuilderService _embeds;
    private readonly BotLogWriter _logWriter;
    private readonly ILogger<TicketArchiveService> _logger;

    public TicketArchiveService(
        BotApiClient apiClient,
        EmbedBuilderService embeds,
        BotLogWriter logWriter,
        ILogger<TicketArchiveService> logger)
    {
        _apiClient = apiClient;
        _embeds = embeds;
        _logWriter = logWriter;
        _logger = logger;
    }

    public async Task TryArchiveTicketAsync(
        DiscordSocketClient client,
        SocketGuild guild,
        Guid ticketId,
        int ticketNumber,
        string ownerDiscordUserId,
        string? ownerDisplayName,
        IUser? closedBy,
        DateTimeOffset? closedAt,
        string? archiveChannelId,
        string? closedByName = null,
        string? closedById = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(archiveChannelId)
            || !ulong.TryParse(archiveChannelId, out var archiveChannelSnowflake))
        {
            return;
        }

        var archiveChannel = guild.GetTextChannel(archiveChannelSnowflake);
        if (archiveChannel is null)
        {
            _logger.LogWarning(
                "Ticket archive channel {ChannelId} not found in guild {GuildId}.",
                archiveChannelId,
                guild.Id);
            return;
        }

        try
        {
            // BR-X03: Archive preview is built from Timeline, not Discord channel history.
            var preview = await BuildTranscriptPreviewFromTimelineAsync(ticketId, cancellationToken);
            var resolvedClosedByName = closedByName
                ?? closedBy?.GlobalName
                ?? closedBy?.Username
                ?? "Unknown";
            var resolvedClosedById = closedById ?? closedBy?.Id.ToString() ?? "—";
            var embed = _embeds.BuildTicketArchive(
                ticketNumber,
                ownerDisplayName ?? ownerDiscordUserId,
                ownerDiscordUserId,
                resolvedClosedByName,
                resolvedClosedById,
                closedAt ?? DateTimeOffset.UtcNow,
                preview);

            await archiveChannel.SendMessageAsync(embed: embed);

            await _apiClient.RecordTicketArchivePostedAsync(
                ticketId,
                new RecordTicketArchivePostedApiRequest
                {
                    ArchiveChannelDiscordId = archiveChannelId,
                    ActorDiscordUserId = resolvedClosedById == "—" ? null : resolvedClosedById,
                    ActorDisplayName = resolvedClosedByName == "Unknown" ? null : resolvedClosedByName
                },
                cancellationToken);

            await _logWriter.WriteAsync(
                guild.Id.ToString(),
                LogEventType.TicketArchived,
                $"Ticket #{ticketNumber} transcript archived.",
                resolvedClosedById == "—" ? null : resolvedClosedById,
                ownerDiscordUserId,
                archiveChannelId,
                resolvedClosedByName == "Unknown" ? null : resolvedClosedByName,
                ownerDisplayName,
                archiveChannel.Name,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to archive ticket #{TicketNumber} in guild {GuildId}.",
                ticketNumber,
                guild.Id);
        }
    }

    public Task TryArchiveFromCleanupAsync(
        DiscordSocketClient client,
        SocketGuild guild,
        TicketCleanupApiResponse item,
        CancellationToken cancellationToken = default) =>
        TryArchiveTicketAsync(
            client,
            guild,
            item.TicketId,
            item.TicketNumber,
            item.OwnerDiscordUserId,
            item.OwnerDisplayName,
            null,
            item.ClosedAt,
            item.TicketArchiveChannelId,
            item.ClosedByDisplayName,
            item.ClosedByDiscordUserId,
            cancellationToken);

    private async Task<string> BuildTranscriptPreviewFromTimelineAsync(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var events = await _apiClient.GetTicketTimelineAsync(ticketId, limit: 100, cancellationToken);
        if (events.Count == 0)
        {
            return "_No messages recorded on the ticket timeline yet._\n_Open the dashboard to view the full timeline._";
        }

        var failedQueuedIds = events
            .Where(e => string.Equals(e.EventType, nameof(TicketTimelineEventType.StaffReplyFailed), StringComparison.Ordinal)
                && e.RelatedTimelineEventId.HasValue)
            .Select(e => e.RelatedTimelineEventId!.Value)
            .ToHashSet();

        var lines = events
            .Where(e => !string.IsNullOrWhiteSpace(e.Content))
            .Where(e =>
                string.Equals(e.EventType, nameof(TicketTimelineEventType.MessageSent), StringComparison.Ordinal)
                || (string.Equals(e.EventType, nameof(TicketTimelineEventType.StaffReplyQueued), StringComparison.Ordinal)
                    && !failedQueuedIds.Contains(e.Id)))
            .Select(e =>
            {
                var name = string.IsNullOrWhiteSpace(e.ActorDisplayName) ? "Unknown" : e.ActorDisplayName;
                var staffSuffix = string.Equals(
                    e.EventType,
                    nameof(TicketTimelineEventType.StaffReplyQueued),
                    StringComparison.Ordinal)
                    ? " (Staff)"
                    : string.Empty;

                return $"**{name}{staffSuffix}:** {Truncate(e.Content!, 200)}";
            })
            .TakeLast(MaxPreviewMessages)
            .ToList();

        if (lines.Count == 0)
        {
            return "_No messages recorded on the ticket timeline yet._\n_Open the dashboard to view the full timeline._";
        }

        var preview = string.Join('\n', lines);
        if (preview.Length > MaxPreviewLength)
        {
            preview = preview[..MaxPreviewLength] + "\n…\n_Open the dashboard to view the full timeline._";
        }
        else
        {
            preview += "\n_Open the dashboard to view the full timeline._";
        }

        return preview;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "…";
}
