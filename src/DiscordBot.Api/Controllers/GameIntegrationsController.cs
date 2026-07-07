using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

[AllowAnonymous, ApiController, Route("api/game-integrations")]
public class GameIntegrationsController(IGamePluginService plugins) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct) => Result(await plugins.ValidateRuntimeTokenAsync(RuntimeToken(), ct));

    [HttpGet("wallet")]
    public async Task<IActionResult> Wallet(CancellationToken ct) => Result(await plugins.GetWalletAsync(RuntimeToken(), ct));

    [HttpPost("wallet/transactions")]
    public async Task<IActionResult> WalletTransaction(RequestGameWalletTransactionRequest request, CancellationToken ct) => Result(await plugins.RequestWalletTransactionAsync(RuntimeToken(), request, ct));

    [HttpPost("events")]
    public async Task<IActionResult> Event(EmitGameEventRequest request, CancellationToken ct) => Result(await plugins.EmitEventAsync(RuntimeToken(), request, ct));

    [HttpPost("bot/publish")]
    public async Task<IActionResult> BotPublish(RequestGameBotPublishRequest request, CancellationToken ct) => Result(await plugins.RequestBotPublishAsync(RuntimeToken(), request, ct));

    private string RuntimeToken()
    {
        var header = Request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..].Trim() : string.Empty;
    }

    private ObjectResult Result<T>(GameHubResult<T> result) => StatusCode(result.StatusCode, result.Succeeded ? result.Value : new { message = result.Error });
}
