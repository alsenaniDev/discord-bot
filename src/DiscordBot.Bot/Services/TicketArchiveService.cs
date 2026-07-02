using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;
using DiscordBot.Bot.Configuration;
using DiscordBot.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBot.Bot.Services;

public class TicketArchiveService
{
    private const int MaxPreviewMessages = 8;
    private const int MaxPreviewLength = 1500;

    private readonly BotApiClient _apiClient;
    private readonly EmbedBuilderService _embeds;
    private readonly BotLogWriter _logWriter;
    private readonly PlatformOptions _platformOptions;
    private readonly ILogger<TicketArchiveService> _logger;

    public TicketArchiveService(
        BotApiClient apiClient,
        EmbedBuilderService embeds,
        BotLogWriter logWriter,
        IOptions<PlatformOptions> platformOptions,
        ILogger<TicketArchiveService> logger)
    {
        _apiClient = apiClient;
        _embeds = embeds;
        _logWriter = logWriter;
        _platformOptions = platformOptions.Value;
        _logger = logger;
    }

    public async Task TryArchiveTicketAsync(
        DiscordSocketClient client,
        SocketGuild guild,
        Guid platformGuildId,
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
            // BR-X03: Archive digest is built from Timeline, not Discord channel history.
            var digestPreview = await BuildArchiveDigestPreviewFromTimelineAsync(ticketId, cancellationToken);
            var resolvedClosedByName = closedByName
                ?? closedBy?.GlobalName
                ?? closedBy?.Username
                ?? "Unknown";
            var resolvedClosedById = closedById ?? closedBy?.Id.ToString() ?? "—";
            var transcriptUrl = BuildTranscriptUrl(platformGuildId, ticketId);
            var embed = _embeds.BuildTicketArchive(
                ticketNumber,
                ownerDisplayName ?? ownerDiscordUserId,
                ownerDiscordUserId,
                resolvedClosedByName,
                resolvedClosedById,
                closedAt ?? DateTimeOffset.UtcNow,
                digestPreview,
                transcriptUrl);

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
                $"Ticket #{ticketNumber} archive digest posted.",
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
            item.GuildId,
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

    private string? BuildTranscriptUrl(Guid platformGuildId, Guid ticketId)
    {
        if (platformGuildId == Guid.Empty)
        {
            return null;
        }

        var baseUrl = _platformOptions.DashboardUrl?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        return $"{baseUrl}/guilds/{platformGuildId}/tickets/{ticketId}/transcript";
    }

    private async Task<string> BuildArchiveDigestPreviewFromTimelineAsync(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var conversation = await _apiClient.GetTicketConversationAsync(ticketId, limit: 100, cancellationToken);
        if (conversation is null || conversation.Items.Count == 0)
        {
            return "_No messages recorded on the ticket timeline yet._";
        }

        var lines = conversation.Items
            .Where(e => !string.IsNullOrWhiteSpace(e.Content))
            .Where(e =>
                string.Equals(e.EventType, "MessageSent", StringComparison.Ordinal)
                || (string.Equals(e.EventType, "StaffReplyQueued", StringComparison.Ordinal)
                    && !string.Equals(e.DeliveryStatus, "Failed", StringComparison.Ordinal)))
            .Select(e =>
            {
                var name = string.IsNullOrWhiteSpace(e.ActorUsername) ? "Unknown" : e.ActorUsername;
                var staffSuffix = string.Equals(e.ActorType, "Staff", StringComparison.Ordinal)
                    && string.Equals(e.EventType, "StaffReplyQueued", StringComparison.Ordinal)
                    ? " (Staff)"
                    : string.Empty;

                return $"**{name}{staffSuffix}:** {Truncate(e.Content!, 200)}";
            })
            .TakeLast(MaxPreviewMessages)
            .ToList();

        if (lines.Count == 0)
        {
            return "_No messages recorded on the ticket timeline yet._";
        }

        var preview = string.Join('\n', lines);
        if (preview.Length > MaxPreviewLength)
        {
            preview = preview[..MaxPreviewLength] + "\n…";
        }

        return preview + "\n_Summary generated from Timeline — not a complete transcript._";
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "…";
}
