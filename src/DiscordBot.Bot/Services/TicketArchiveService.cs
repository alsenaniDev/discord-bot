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
        ITextChannel ticketChannel,
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
            var preview = await BuildTranscriptPreviewAsync(ticketChannel, cancellationToken);
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
        ITextChannel ticketChannel,
        TicketCleanupApiResponse item,
        CancellationToken cancellationToken = default) =>
        TryArchiveTicketAsync(
            client,
            guild,
            ticketChannel,
            item.TicketNumber,
            item.OwnerDiscordUserId,
            item.OwnerDisplayName,
            null,
            item.ClosedAt,
            item.TicketArchiveChannelId,
            item.ClosedByDisplayName,
            item.ClosedByDiscordUserId,
            cancellationToken);

    private static async Task<string> BuildTranscriptPreviewAsync(
        ITextChannel channel,
        CancellationToken cancellationToken)
    {
        var messages = await channel.GetMessagesAsync(limit: MaxPreviewMessages).FlattenAsync();
        var lines = messages
            .Reverse()
            .Where(m => !string.IsNullOrWhiteSpace(m.Content))
            .Select(m => $"**{m.Author.Username}:** {Truncate(m.Content, 200)}")
            .Take(MaxPreviewMessages)
            .ToList();

        if (lines.Count == 0)
        {
            return "_No text messages in this ticket._\nFull ticket details are available in the dashboard.";
        }

        var preview = string.Join('\n', lines);
        if (preview.Length > MaxPreviewLength)
        {
            preview = preview[..MaxPreviewLength] + "\n…\n_Full ticket is available in the dashboard._";
        }
        else
        {
            preview += "\n_Full ticket is available in the dashboard._";
        }

        return preview;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "…";
}
