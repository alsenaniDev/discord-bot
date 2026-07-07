using DiscordBot.Api.Filters;
using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

[Authorize, PlatformAdmin, ApiController, Route("api/admin/games")]
public class AdminGamesController(IGameHubService games, IGamePluginService plugins) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => Ok(await games.GetCatalogAsync(ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct) => (await games.GetCatalogGameAsync(id, ct)) is { } value ? Ok(value) : NotFound(new { message = "اللعبة غير موجودة." });
    [HttpPost] public async Task<IActionResult> Create(SavePlatformGameDefinitionRequest request, CancellationToken ct) => Result(await games.CreateCatalogGameAsync(request, ct));
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, SavePlatformGameDefinitionRequest request, CancellationToken ct) => Result(await games.UpdateCatalogGameAsync(id, request, ct));
    [HttpPatch("{id:guid}/toggle")] public async Task<IActionResult> Toggle(Guid id, CancellationToken ct) => (await games.ToggleCatalogGameAsync(id, ct)) is { } value ? Ok(value) : NotFound(new { message = "اللعبة غير موجودة." });
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) => await games.DisableCatalogGameAsync(id, ct) ? NoContent() : NotFound(new { message = "اللعبة غير موجودة." });
    [HttpGet("{id:guid}/versions")] public async Task<IActionResult> Versions(Guid id, CancellationToken ct) => Ok(await plugins.GetVersionsAsync(id, ct));
    [HttpPost("{id:guid}/versions")] public async Task<IActionResult> CreateVersion(Guid id, CreateGameVersionRequest request, CancellationToken ct) => Result(await plugins.CreateVersionAsync(id, request, ct));
    [HttpPatch("versions/{versionId:guid}/status")] public async Task<IActionResult> UpdateVersionStatus(Guid versionId, UpdateGameVersionStatusRequest request, CancellationToken ct) => Result(await plugins.UpdateVersionStatusAsync(versionId, request, ct));
    [HttpPost("versions/{versionId:guid}/sandbox-access")] public async Task<IActionResult> AddSandboxAccess(Guid versionId, AddGameSandboxAccessRequest request, CancellationToken ct) => Result(await plugins.AddSandboxAccessAsync(versionId, request, ct));
    [HttpDelete("versions/sandbox-access/{accessId:guid}")] public async Task<IActionResult> RemoveSandboxAccess(Guid accessId, CancellationToken ct) => await plugins.RemoveSandboxAccessAsync(accessId, ct) ? NoContent() : NotFound(new { message = "صلاحية الاختبار غير موجودة." });
    private ObjectResult Result<T>(GameHubResult<T> result) => StatusCode(result.StatusCode, result.Succeeded ? result.Value : new { message = result.Error });
}
