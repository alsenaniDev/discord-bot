using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Services;
using DiscordBot.Api.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

[AllowAnonymous]
[BotApiKey]
[ApiController]
[Route("api/bot/reaction-roles")]
public class BotReactionRolesController : ControllerBase
{
    private readonly IReactionRoleService _reactionRoleService;

    public BotReactionRolesController(IReactionRoleService reactionRoleService)
    {
        _reactionRoleService = reactionRoleService;
    }

    [HttpPost]
    public async Task<ActionResult<ReactionRoleDto>> Create(
        [FromBody] CreateReactionRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DiscordGuildId)
            || string.IsNullOrWhiteSpace(request.ChannelDiscordId)
            || string.IsNullOrWhiteSpace(request.MessageDiscordId)
            || string.IsNullOrWhiteSpace(request.RoleDiscordId)
            || string.IsNullOrWhiteSpace(request.ButtonCustomId)
            || string.IsNullOrWhiteSpace(request.Title)
            || string.IsNullOrWhiteSpace(request.Description)
            || string.IsNullOrWhiteSpace(request.ButtonLabel))
        {
            return BadRequest(new { message = "All reaction role fields are required." });
        }

        var created = await _reactionRoleService.CreateAsync(request, cancellationToken);
        if (created is null)
        {
            return BadRequest(new { message = "Could not create reaction role. Guild not found or button ID already exists." });
        }

        return Ok(created);
    }

    [HttpGet("by-button/{customId}")]
    public async Task<ActionResult<ReactionRoleDto>> GetByButtonCustomId(
        string customId,
        CancellationToken cancellationToken)
    {
        var reactionRole = await _reactionRoleService.GetByButtonCustomIdAsync(customId, cancellationToken);
        if (reactionRole is null)
        {
            return NotFound(new { message = "Reaction role not found." });
        }

        return Ok(reactionRole);
    }
}
