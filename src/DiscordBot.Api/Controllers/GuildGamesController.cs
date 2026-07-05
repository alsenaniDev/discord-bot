using DiscordBot.Api.Extensions;
using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

[Authorize, ApiController, Route("api/guilds/{guildId:guid}/games")]
public class GuildGamesController(IGameHubService games, IGuildAccessService access) : ControllerBase
{
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(Guid guildId, CancellationToken ct) => !await Allowed(guildId, ct) ? NotFound() : (await games.GetGuildSettingsAsync(guildId, ct)) is { } value ? Ok(value) : NotFound();

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings(Guid guildId, UpdateGuildGamesSettingsRequest request, CancellationToken ct) => !await Allowed(guildId, ct) ? NotFound() : Result(await games.UpdateGuildSettingsAsync(guildId, request, ct));

    [HttpGet]
    public async Task<IActionResult> List(Guid guildId, CancellationToken ct) => !await Allowed(guildId, ct) ? NotFound() : (await games.GetGuildGamesAsync(guildId, ct)) is { } value ? Ok(value) : NotFound();

    [HttpPut("{platformGameDefinitionId:guid}/settings")]
    public async Task<IActionResult> UpdateGame(Guid guildId, Guid platformGameDefinitionId, UpdateGuildGameSettingRequest request, CancellationToken ct) => !await Allowed(guildId, ct) ? NotFound() : Result(await games.UpdateGuildGameAsync(guildId, platformGameDefinitionId, request, ct));

    [HttpGet("leaderboard")]
    public async Task<IActionResult> Leaderboard(Guid guildId, [FromQuery] Guid? gameId, [FromQuery] int limit = 10, CancellationToken ct = default) => !await Allowed(guildId, ct) ? NotFound() : (await games.GetLeaderboardAsync(guildId, gameId, limit, ct)) is { } value ? Ok(value) : NotFound();

    private async Task<bool> Allowed(Guid guildId, CancellationToken ct)
    {
        var userId = User.GetDiscordUserId(); return !string.IsNullOrWhiteSpace(userId) && (await access.GetAccessAsync(guildId, userId, ct))?.CanManageSettings == true;
    }
    private ObjectResult Result<T>(GameHubResult<T> result) => StatusCode(result.StatusCode, result.Succeeded ? result.Value : new { message = result.Error });
}
