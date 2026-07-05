using DiscordBot.Api.Extensions;
using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

[Authorize, ApiController]
[Route("api/dashboard/guilds/{guildId:guid}/music-settings")]
[Route("api/guilds/{guildId:guid}/music-settings")]
public class GuildMusicSettingsController(IMusicSettingsService music, IGuildAccessService access) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid guildId, CancellationToken cancellationToken)
    {
        if (!await Allowed(guildId, cancellationToken)) return NotFound();
        var value = await music.GetAsync(guildId, cancellationToken);
        return value is null ? NotFound() : Ok(value);
    }

    [HttpPut]
    public async Task<IActionResult> Update(Guid guildId, UpdateGuildMusicSettingsRequest request, CancellationToken cancellationToken)
    {
        if (!await Allowed(guildId, cancellationToken)) return NotFound();
        var result = await music.UpdateAsync(guildId, request, cancellationToken);
        if (result.Error is not null) return BadRequest(new { message = result.Error });
        return result.Value is null ? NotFound() : Ok(result.Value);
    }

    private async Task<bool> Allowed(Guid guildId, CancellationToken cancellationToken)
    {
        var userId = User.GetDiscordUserId();
        return !string.IsNullOrWhiteSpace(userId) && (await access.GetAccessAsync(guildId, userId, cancellationToken))?.CanManageSettings == true;
    }
}
