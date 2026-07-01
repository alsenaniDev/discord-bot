using DiscordBot.Domain.Enums;
using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Services;
using DiscordBot.Api.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

[AllowAnonymous]
[BotApiKey]
[ApiController]
[Route("api/bot/logs")]
public class BotLogsController : ControllerBase
{
    private readonly ILogService _logService;

    public BotLogsController(ILogService logService)
    {
        _logService = logService;
    }

    [HttpPost]
    public async Task<ActionResult<LogEntryDto>> CreateLog(
        [FromBody] BotCreateLogRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DiscordGuildId)
            || string.IsNullOrWhiteSpace(request.Message)
            || string.IsNullOrWhiteSpace(request.Type))
        {
            return BadRequest(new { message = "DiscordGuildId, Type, and Message are required." });
        }

        if (!Enum.TryParse<LogEventType>(request.Type, ignoreCase: true, out var type))
        {
            return BadRequest(new { message = $"Unknown log type '{request.Type}'." });
        }

        var entry = await _logService.CreateLogAsync(new CreateLogRequest
        {
            DiscordGuildId = request.DiscordGuildId,
            Type = type,
            Message = request.Message,
            ActorDiscordUserId = request.ActorDiscordUserId,
            TargetDiscordUserId = request.TargetDiscordUserId,
            ChannelDiscordId = request.ChannelDiscordId,
            ActorDisplayName = request.ActorDisplayName,
            TargetDisplayName = request.TargetDisplayName,
            ChannelDisplayName = request.ChannelDisplayName,
            MetadataJson = request.MetadataJson
        }, cancellationToken);

        if (entry is null)
        {
            return Accepted();
        }

        return Ok(entry);
    }
}

public sealed class BotCreateLogRequest
{
    public required string DiscordGuildId { get; set; }
    public required string Type { get; set; }
    public required string Message { get; set; }
    public string? ActorDiscordUserId { get; set; }
    public string? TargetDiscordUserId { get; set; }
    public string? ChannelDiscordId { get; set; }
    public string? ActorDisplayName { get; set; }
    public string? TargetDisplayName { get; set; }
    public string? ChannelDisplayName { get; set; }
    public string? MetadataJson { get; set; }
}
