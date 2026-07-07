using DiscordBot.Api.Filters;
using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

[AllowAnonymous, BotApiKey, ApiController, Route("api/bot/games")]
public class BotGamesController(IGameHubService games, IRouletteService roulette, IGamePluginService plugins) : ControllerBase
{
    [HttpGet("context/{discordGuildId}")] public async Task<IActionResult> Context(string discordGuildId, CancellationToken ct) => Ok(await games.GetBotContextAsync(discordGuildId, ct));
    [HttpGet("leaderboard/{discordGuildId}")] public async Task<IActionResult> Leaderboard(string discordGuildId, [FromQuery] int limit = 10, CancellationToken ct = default) => (await games.GetLeaderboardByDiscordGuildIdAsync(discordGuildId, limit, ct)) is { } value ? Ok(value) : NotFound();
    [HttpGet("publish-actions/pending")] public async Task<IActionResult> Pending(CancellationToken ct) => Ok(await games.GetPendingPublishActionsAsync(ct));
    [HttpPost("publish-actions/{id:guid}/ack")] public async Task<IActionResult> Ack(Guid id, AckGamePublishActionRequest request, CancellationToken ct) => await games.AckPublishActionAsync(id, request, ct) ? Ok() : NotFound();
    [HttpGet("generic-publish-actions/pending")] public async Task<IActionResult> PendingGeneric(CancellationToken ct) => Ok(await plugins.GetPendingBotPublishActionsAsync(ct));
    [HttpPost("generic-publish-actions/{id:guid}/ack")] public async Task<IActionResult> AckGeneric(Guid id, AckGameBotPublishActionRequest request, CancellationToken ct) => await plugins.AckBotPublishActionAsync(id, request, ct) ? Ok() : NotFound();
    [HttpPost("roulette/rooms/{roomId:guid}/prepare-join")] public async Task<IActionResult> PrepareRouletteJoin(Guid roomId, PrepareRouletteJoinRequest request, CancellationToken ct) => Result(await roulette.PrepareJoinAsync(roomId, request, ct));
    [HttpGet("roulette/publish-actions/pending")] public async Task<IActionResult> PendingRoulette(CancellationToken ct) => Ok(await roulette.GetPendingPublishActionsAsync(ct));
    [HttpPost("roulette/publish-actions/{id:guid}/ack")] public async Task<IActionResult> AckRoulette(Guid id, AckRoulettePublishActionRequest request, CancellationToken ct) => await roulette.AckPublishActionAsync(id, request, ct) ? Ok() : NotFound();

    private ObjectResult Result<T>(GameHubResult<T> result) => StatusCode(result.StatusCode, result.Succeeded ? result.Value : new { message = result.Error });
}
