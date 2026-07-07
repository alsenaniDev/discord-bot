using DiscordBot.Infrastructure.Auth;
using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

[AllowAnonymous, ApiController, Route("api/games/activity")]
public class ActivityGamesController(IDiscordActivityAuthService auth, IGameHubService games, IRouletteService roulette) : ControllerBase
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

    [HttpGet("wallet")]
    public async Task<IActionResult> Wallet([FromQuery] string guildDiscordId, CancellationToken ct)
    {
        var user = await UserAsync(ct); if (user is null) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        return Result(await roulette.GetWalletAsync(guildDiscordId, user.Id, ct));
    }

    [HttpGet("store")]
    public async Task<IActionResult> Store([FromQuery] string guildDiscordId, CancellationToken ct)
    {
        var user = await UserAsync(ct); if (user is null) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        return Result(await roulette.GetStoreAsync(guildDiscordId, user.Id, ct));
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> Inventory([FromQuery] string guildDiscordId, CancellationToken ct)
    {
        var user = await UserAsync(ct); if (user is null) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        var result = await roulette.GetStoreAsync(guildDiscordId, user.Id, ct);
        return StatusCode(result.StatusCode, result.Succeeded ? result.Value?.Items : new { message = result.Error });
    }

    [HttpPost("store/purchase")]
    public async Task<IActionResult> Purchase(PurchasePowerUpRequest request, CancellationToken ct)
    {
        var user = await UserAsync(ct); if (user is null) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        return Result(await roulette.PurchasePowerUpAsync(request, user.Id, ct));
    }

    [HttpPost("roulette/rooms")]
    public async Task<IActionResult> CreateRouletteRoom(CreateRouletteRoomRequest request, CancellationToken ct)
    {
        var user = await UserAsync(ct); if (user is null) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        return Result(await roulette.CreateRoomAsync(request, user.Id, user.Username, user.GlobalName, user.AvatarUrl, ct));
    }

    [HttpGet("roulette/rooms/open")]
    public async Task<IActionResult> OpenRouletteRooms([FromQuery] string guildDiscordId, [FromQuery] string channelDiscordId, CancellationToken ct)
    {
        var user = await UserAsync(ct); if (user is null) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        return Result(await roulette.GetOpenRoomsAsync(guildDiscordId, channelDiscordId, user.Id, ct));
    }

    [HttpGet("roulette/my-active-room")]
    public async Task<IActionResult> MyActiveRouletteRoom([FromQuery] string guildDiscordId, [FromQuery] string channelDiscordId, CancellationToken ct)
    {
        var user = await UserAsync(ct); if (user is null) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        return Result(await roulette.GetMyActiveRoomAsync(guildDiscordId, channelDiscordId, user.Id, ct));
    }

    [HttpGet("roulette/rooms/{roomId:guid}")]
    public async Task<IActionResult> RouletteRoom(Guid roomId, [FromQuery] string guildDiscordId, [FromQuery] string channelDiscordId, CancellationToken ct)
    {
        var user = await UserAsync(ct); if (user is null) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        return Result(await roulette.GetRoomAsync(roomId, guildDiscordId, channelDiscordId, user.Id, ct));
    }

    [HttpPost("roulette/rooms/{roomId:guid}/join")]
    public async Task<IActionResult> JoinRouletteRoom(Guid roomId, CreateRouletteRoomRequest request, CancellationToken ct)
    {
        var user = await UserAsync(ct); if (user is null) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        return Result(await roulette.JoinRoomAsync(roomId, request.GuildDiscordId, request.ChannelDiscordId, user.Id, user.Username, user.GlobalName, user.AvatarUrl, ct));
    }

    [HttpPost("roulette/rooms/{roomId:guid}/leave")]
    public async Task<IActionResult> LeaveRouletteRoom(Guid roomId, CreateRouletteRoomRequest request, CancellationToken ct)
    {
        var user = await UserAsync(ct); if (user is null) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        return Result(await roulette.LeaveRoomAsync(roomId, request.GuildDiscordId, request.ChannelDiscordId, user.Id, ct));
    }

    [HttpPost("roulette/rooms/{roomId:guid}/start")]
    public async Task<IActionResult> StartRouletteRoom(Guid roomId, CreateRouletteRoomRequest request, CancellationToken ct)
    {
        var user = await UserAsync(ct); if (user is null) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        return Result(await roulette.StartRoomAsync(roomId, request.GuildDiscordId, request.ChannelDiscordId, user.Id, ct));
    }

    [HttpPost("roulette/rooms/{roomId:guid}/spin")]
    public async Task<IActionResult> SpinRoulette(Guid roomId, CreateRouletteRoomRequest request, CancellationToken ct)
    {
        var user = await UserAsync(ct); if (user is null) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        return Result(await roulette.SpinAsync(roomId, request.GuildDiscordId, request.ChannelDiscordId, user.Id, ct));
    }

    [HttpPost("roulette/rooms/{roomId:guid}/use-power-up")]
    public async Task<IActionResult> UseRoulettePowerUp(Guid roomId, UsePowerUpRequest request, CancellationToken ct)
    {
        var user = await UserAsync(ct); if (user is null) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        return Result(await roulette.UsePowerUpAsync(roomId, request, user.Id, ct));
    }

    [HttpPost("roulette/rooms/{roomId:guid}/resolve-pending-action")]
    public async Task<IActionResult> ResolveRoulettePendingAction(Guid roomId, CreateRouletteRoomRequest request, CancellationToken ct)
    {
        var user = await UserAsync(ct); if (user is null) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        return Result(await roulette.ResolvePendingActionAsync(roomId, request, user.Id, ct));
    }

    [HttpGet("roulette/pending-intent")]
    public async Task<IActionResult> PendingRouletteIntent([FromQuery] string guildDiscordId, [FromQuery] string channelDiscordId, CancellationToken ct)
    {
        var user = await UserAsync(ct); if (user is null) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        return Result(await roulette.ConsumePendingIntentAsync(guildDiscordId, channelDiscordId, user.Id, ct));
    }

    private async Task<ActivityDiscordUser?> UserAsync(CancellationToken ct)
    {
        var header = Request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? await auth.ValidateAccessTokenAsync(header[7..].Trim(), ct) : null;
    }
    private ObjectResult Result<T>(GameHubResult<T> result) => StatusCode(result.StatusCode, result.Succeeded ? result.Value : new { message = result.Error });
}
