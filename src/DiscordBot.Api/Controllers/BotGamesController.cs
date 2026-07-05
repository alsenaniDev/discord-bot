using DiscordBot.Api.Filters;
using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

[AllowAnonymous, BotApiKey, ApiController, Route("api/bot/games")]
public class BotGamesController(IGameHubService games) : ControllerBase
{
    [HttpGet("context/{discordGuildId}")] public async Task<IActionResult> Context(string discordGuildId, CancellationToken ct) => Ok(await games.GetBotContextAsync(discordGuildId, ct));
    [HttpGet("leaderboard/{discordGuildId}")] public async Task<IActionResult> Leaderboard(string discordGuildId, [FromQuery] int limit = 10, CancellationToken ct = default) => (await games.GetLeaderboardByDiscordGuildIdAsync(discordGuildId, limit, ct)) is { } value ? Ok(value) : NotFound();
    [HttpGet("publish-actions/pending")] public async Task<IActionResult> Pending(CancellationToken ct) => Ok(await games.GetPendingPublishActionsAsync(ct));
    [HttpPost("publish-actions/{id:guid}/ack")] public async Task<IActionResult> Ack(Guid id, AckGamePublishActionRequest request, CancellationToken ct) => await games.AckPublishActionAsync(id, request, ct) ? Ok() : NotFound();
}
