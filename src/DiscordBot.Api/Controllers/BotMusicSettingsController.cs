using DiscordBot.Api.Filters;
using DiscordBot.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

[AllowAnonymous, BotApiKey, ApiController, Route("api/bot/guilds/{discordGuildId}/music-settings")]
public class BotMusicSettingsController(IMusicSettingsService music) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string discordGuildId, CancellationToken cancellationToken)
    {
        var value = await music.GetByDiscordGuildIdAsync(discordGuildId, cancellationToken);
        return value is null ? NotFound() : Ok(value);
    }
}
