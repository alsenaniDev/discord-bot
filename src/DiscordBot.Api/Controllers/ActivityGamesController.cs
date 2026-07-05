using DiscordBot.Infrastructure.Auth;
using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

[AllowAnonymous, ApiController, Route("api/games/activity")]
public class ActivityGamesController(IDiscordActivityAuthService auth, IGameHubService games) : ControllerBase
{
    [HttpGet("context")]
    public async Task<IActionResult> Context([FromQuery] string guildDiscordId, [FromQuery] string channelDiscordId, CancellationToken ct)
    {
        if (await UserAsync(ct) is null) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        return Result(await games.GetActivityContextAsync(guildDiscordId, channelDiscordId, ct));
    }

    [HttpPost("start-session")]
    public async Task<IActionResult> Start(ActivityStartGameSessionRequest request, CancellationToken ct)
    {
        var user = await UserAsync(ct); if (user is null) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        return Result(await games.StartSessionAsync(new StartGameSessionRequest { GuildDiscordId = request.GuildDiscordId, ChannelDiscordId = request.ChannelDiscordId, UserDiscordId = user.Id, Username = user.GlobalName ?? user.Username, GameKey = request.GameKey }, ct));
    }

    [HttpPost("complete-session")]
    public async Task<IActionResult> Complete(ActivityCompleteGameSessionRequest request, CancellationToken ct)
    {
        var user = await UserAsync(ct); if (user is null) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        return Result(await games.CompleteSessionAsync(new CompleteGameSessionRequest { SessionId = request.SessionId, GuildDiscordId = request.GuildDiscordId, UserDiscordId = user.Id, Score = request.Score, Won = request.Won }, ct));
    }

    [HttpGet("leaderboard")]
    public async Task<IActionResult> Leaderboard([FromQuery] string guildDiscordId, [FromQuery] string channelDiscordId, [FromQuery] string? gameKey, [FromQuery] int limit = 10, CancellationToken ct = default)
    {
        if (await UserAsync(ct) is null) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        return Result(await games.GetActivityLeaderboardAsync(guildDiscordId, channelDiscordId, gameKey, limit, ct));
    }

    private async Task<ActivityDiscordUser?> UserAsync(CancellationToken ct)
    {
        var header = Request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? await auth.ValidateAccessTokenAsync(header[7..].Trim(), ct) : null;
    }
    private ObjectResult Result<T>(GameHubResult<T> result) => StatusCode(result.StatusCode, result.Succeeded ? result.Value : new { message = result.Error });
}
