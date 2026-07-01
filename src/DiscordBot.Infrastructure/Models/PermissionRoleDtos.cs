using DiscordBot.Domain.Enums;

namespace DiscordBot.Infrastructure.Models;

public sealed class GuildPermissionRoleDto
{
    public Guid Id { get; init; }
    public Guid GuildId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DiscordRoleId { get; init; } = string.Empty;
    public string? DiscordRoleName { get; init; }
    public GuildPermissions Permissions { get; init; }
    public IReadOnlyList<string> PermissionKeys { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class CreateGuildPermissionRoleRequest
{
    public required string Name { get; set; }
    public required string DiscordRoleId { get; set; }
    public List<string> PermissionKeys { get; set; } = [];
}

public sealed class UpdateGuildPermissionRoleRequest
{
    public required string Name { get; set; }
    public required string DiscordRoleId { get; set; }
    public List<string> PermissionKeys { get; set; } = [];
}

public sealed class EvaluatePermissionsRequest
{
    public required string DiscordUserId { get; set; }
    public List<string> DiscordRoleIds { get; set; } = [];
}

public sealed class EvaluatePermissionsResponse
{
    public GuildPermissions Permissions { get; init; }
    public bool CanWarn { get; init; }
    public bool CanKick { get; init; }
    public bool CanTimeout { get; init; }
    public bool CanClearMessages { get; init; }
    public bool CanAccessModeration { get; init; }
}
