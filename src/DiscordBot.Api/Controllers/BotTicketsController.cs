using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Services;
using DiscordBot.Api.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

[AllowAnonymous]
[BotApiKey]
[ApiController]
[Route("api/bot/tickets")]
public class BotTicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public BotTicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpPost]
    public async Task<ActionResult<TicketDto>> CreateTicket(
        [FromBody] CreateTicketRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DiscordGuildId)
            || string.IsNullOrWhiteSpace(request.OwnerDiscordUserId)
            || string.IsNullOrWhiteSpace(request.ChannelDiscordId))
        {
            return BadRequest(new { message = "DiscordGuildId, OwnerDiscordUserId, and ChannelDiscordId are required." });
        }

        try
        {
            var ticket = await _ticketService.CreateTicketAsync(request, cancellationToken);
            if (ticket is null)
            {
                return BadRequest(new { message = "Tickets are not enabled for this server. Run /ticket setup first." });
            }

            return Ok(ticket);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("by-channel/{channelDiscordId}")]
    public async Task<ActionResult<TicketDto>> GetByChannel(
        string channelDiscordId,
        CancellationToken cancellationToken)
    {
        var ticket = await _ticketService.GetByChannelDiscordIdAsync(channelDiscordId, cancellationToken);
        if (ticket is null)
        {
            return NotFound(new { message = "Ticket not found for this channel." });
        }

        return Ok(ticket);
    }

    [HttpPatch("{id:guid}/close")]
    public async Task<ActionResult<TicketDto>> CloseTicket(
        Guid id,
        [FromBody] CloseTicketRequest? request,
        CancellationToken cancellationToken)
    {
        var ticket = await _ticketService.CloseTicketAsync(id, request, cancellationToken);
        if (ticket is null)
        {
            return NotFound(new { message = "Ticket not found or already closed." });
        }

        return Ok(ticket);
    }

    [HttpGet("pending-cleanups")]
    public async Task<ActionResult<IReadOnlyList<TicketChannelCleanupDto>>> GetPendingCleanups(
        CancellationToken cancellationToken)
    {
        var pending = await _ticketService.GetPendingChannelCleanupsAsync(cancellationToken);
        return Ok(pending);
    }

    [HttpPost("{ticketId:guid}/ack-cleanup")]
    public async Task<IActionResult> AcknowledgeCleanup(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var success = await _ticketService.AcknowledgeChannelCleanupAsync(ticketId, cancellationToken);
        if (!success)
        {
            return NotFound(new { message = "Ticket not found." });
        }

        return Ok(new { message = "Ticket channel cleanup acknowledged." });
    }

    [HttpGet("pending-messages")]
    public async Task<ActionResult<IReadOnlyList<PendingTicketMessageDto>>> GetPendingMessages(
        CancellationToken cancellationToken)
    {
        var pending = await _ticketService.GetPendingOutboundMessagesAsync(cancellationToken);
        return Ok(pending);
    }

    [HttpPost("messages/{messageId:guid}/ack")]
    public async Task<IActionResult> AcknowledgeMessage(
        Guid messageId,
        [FromBody] AcknowledgeTicketMessageDeliveryRequest? request,
        CancellationToken cancellationToken)
    {
        var success = await _ticketService.AcknowledgeOutboundMessageAsync(messageId, request, cancellationToken);
        if (!success)
        {
            return NotFound(new { message = "Outbound message not found or already processed." });
        }

        return Ok(new { message = "Outbound ticket message acknowledged." });
    }

    [HttpPost("timeline/message-sent")]
    public async Task<ActionResult<TicketTimelineEventDto>> RecordMessageSent(
        [FromBody] RecordTicketMessageSentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ChannelDiscordId)
            || string.IsNullOrWhiteSpace(request.DiscordMessageId)
            || string.IsNullOrWhiteSpace(request.AuthorDiscordUserId)
            || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { message = "ChannelDiscordId, DiscordMessageId, AuthorDiscordUserId, and Content are required." });
        }

        var timelineEvent = await _ticketService.RecordDiscordMessageSentAsync(request, cancellationToken);
        if (timelineEvent is null)
        {
            return NotFound(new { message = "Open ticket not found for channel, or message already recorded." });
        }

        return Ok(timelineEvent);
    }

    [HttpPost("{ticketId:guid}/timeline/archive-posted")]
    public async Task<ActionResult<TicketTimelineEventDto>> RecordArchivePosted(
        Guid ticketId,
        [FromBody] RecordTicketArchivePostedRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ArchiveChannelDiscordId))
        {
            return BadRequest(new { message = "ArchiveChannelDiscordId is required." });
        }

        var timelineEvent = await _ticketService.RecordArchivePostedAsync(ticketId, request, cancellationToken);
        if (timelineEvent is null)
        {
            return NotFound(new { message = "Ticket not found." });
        }

        return Ok(timelineEvent);
    }

    [HttpGet("{ticketId:guid}/timeline")]
    public async Task<ActionResult<IReadOnlyList<TicketTimelineEventDto>>> GetTimelineForBot(
        Guid ticketId,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var timeline = await _ticketService.GetTicketTimelineForBotAsync(ticketId, limit, cancellationToken);
        return Ok(timeline);
    }
}

[AllowAnonymous]
[BotApiKey]
[ApiController]
[Route("api/bot/guilds/{discordGuildId}/tickets")]
public class BotTicketSetupController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public BotTicketSetupController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpPost("setup")]
    public async Task<IActionResult> SetupTickets(
        string discordGuildId,
        [FromBody] SetupTicketsRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TicketCategoryId))
        {
            return BadRequest(new { message = "TicketCategoryId is required." });
        }

        var success = await _ticketService.SetupTicketsAsync(
            discordGuildId,
            request.TicketCategoryId,
            cancellationToken);

        if (!success)
        {
            return NotFound(new { message = "Guild not found." });
        }

        return Ok(new { message = "Ticket system enabled." });
    }
}
