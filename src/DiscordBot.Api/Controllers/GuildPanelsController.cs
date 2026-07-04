using DiscordBot.Api.Extensions;
using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/guilds/{guildId:guid}/panels")]
public class GuildPanelsController : ControllerBase
{
    private readonly ICommandPanelService _panels;
    private readonly IGuildAccessService _access;

    public GuildPanelsController(ICommandPanelService panels, IGuildAccessService access)
    {
        _panels = panels;
        _access = access;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GuildPanelDto>>> List(Guid guildId, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(guildId, cancellationToken)) return NotFound(new { message = "Guild not found or access denied." });
        return Ok(await _panels.GetGuildPanelsAsync(guildId, cancellationToken));
    }

    [HttpGet("{panelId:guid}")]
    public async Task<ActionResult<GuildPanelDto>> Get(Guid guildId, Guid panelId, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(guildId, cancellationToken)) return NotFound(new { message = "Guild not found or access denied." });
        var panel = await _panels.GetGuildPanelAsync(guildId, panelId, cancellationToken);
        return panel is null ? NotFound(new { message = "Panel not found." }) : Ok(panel);
    }

    [HttpPost]
    public async Task<ActionResult<GuildPanelDto>> Create(Guid guildId, [FromBody] SaveGuildPanelRequest request, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(guildId, cancellationToken)) return NotFound(new { message = "Guild not found or access denied." });
        var result = await _panels.CreateAsync(guildId, request, cancellationToken);
        if (result.Error is not null) return BadRequest(new { message = result.Error });
        return CreatedAtAction(nameof(Get), new { guildId, panelId = result.Panel!.Id }, result.Panel);
    }

    [HttpPut("{panelId:guid}")]
    public async Task<ActionResult<GuildPanelDto>> Update(Guid guildId, Guid panelId, [FromBody] SaveGuildPanelRequest request, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(guildId, cancellationToken)) return NotFound(new { message = "Guild not found or access denied." });
        var result = await _panels.UpdateAsync(guildId, panelId, request, cancellationToken);
        if (result.Error is not null) return BadRequest(new { message = result.Error });
        return result.Panel is null ? NotFound(new { message = "Panel not found." }) : Ok(result.Panel);
    }

    [HttpDelete("{panelId:guid}")]
    public async Task<IActionResult> Delete(Guid guildId, Guid panelId, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(guildId, cancellationToken)) return NotFound(new { message = "Guild not found or access denied." });
        return await _panels.DeleteAsync(guildId, panelId, cancellationToken) ? NoContent() : NotFound(new { message = "Panel not found." });
    }

    [HttpPost("{panelId:guid}/publish")]
    public async Task<IActionResult> Publish(Guid guildId, Guid panelId, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(guildId, cancellationToken)) return NotFound(new { message = "Guild not found or access denied." });
        var result = await _panels.RequestPublishAsync(guildId, panelId, cancellationToken);
        if (!result.Found) return NotFound(new { message = "Panel not found." });
        if (result.Error is not null) return BadRequest(new { message = result.Error });
        return Accepted(new { message = "Panel queued for Discord publishing.", status = "PendingPublish" });
    }

    [HttpPost("{panelId:guid}/unpublish")]
    public async Task<IActionResult> Unpublish(Guid guildId, Guid panelId, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(guildId, cancellationToken)) return NotFound(new { message = "Guild not found or access denied." });
        return await _panels.UnpublishAsync(guildId, panelId, cancellationToken) ? Ok(new { message = "Panel disabled." }) : NotFound(new { message = "Panel not found." });
    }

    private async Task<bool> CanManageAsync(Guid guildId, CancellationToken cancellationToken)
    {
        var userId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(userId)) return false;
        var access = await _access.GetAccessAsync(guildId, userId, cancellationToken);
        return access?.CanManageSettings == true;
    }
}
