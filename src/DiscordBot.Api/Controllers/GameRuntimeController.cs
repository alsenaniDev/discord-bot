using DiscordBot.Api.Filters;
using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

// Phase 1 runtime is bot-only. A future Discord Activity must use a signed Activity token,
// never anonymous client-provided guild/user identifiers.
[AllowAnonymous, BotApiKey, ApiController, Route("api/games/runtime")]
public class GameRuntimeController(IGameHubService games) : ControllerBase
{
    [HttpPost("start-session")] public async Task<IActionResult> Start(StartGameSessionRequest request, CancellationToken ct) => Result(await games.StartSessionAsync(request, ct));
    [HttpPost("complete-session")] public async Task<IActionResult> Complete(CompleteGameSessionRequest request, CancellationToken ct) => Result(await games.CompleteSessionAsync(request, ct));
    [HttpGet("guild-by-discord/{guildDiscordId}/available-games")]
    public async Task<IActionResult> Available(string guildDiscordId, CancellationToken ct) => (await games.GetAvailableGamesAsync(guildDiscordId, ct)) is { } value ? Ok(value) : NotFound(new { message = "السيرفر غير موجود أو الألعاب غير مفعّلة." });
    private ObjectResult Result<T>(GameHubResult<T> result) => StatusCode(result.StatusCode, result.Succeeded ? result.Value : new { message = result.Error });
}
