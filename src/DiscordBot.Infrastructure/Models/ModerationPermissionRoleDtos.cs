namespace DiscordBot.Infrastructure.Models;

public sealed class ModerationPermissionRoleDto
{
    public Guid Id { get; init; }
    public Guid GuildId { get; init; }
    public string RoleDiscordId { get; init; } = string.Empty;
    public string? RoleName { get; init; }
    public bool CanWarn { get; init; }
    public bool CanViewWarnings { get; init; }
    public bool CanClearMessages { get; init; }
    public bool CanKick { get; init; }
    public bool CanViewModerationCases { get; init; }
    public bool CanViewLogs { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed class CreateModerationPermissionRoleRequest
{
    public required string RoleDiscordId { get; set; }
    public bool CanWarn { get; set; }
    public bool CanViewWarnings { get; set; }
    public bool CanClearMessages { get; set; }
    public bool CanKick { get; set; }
    public bool CanViewModerationCases { get; set; }
    public bool CanViewLogs { get; set; }
}

public sealed class UpdateModerationPermissionRoleRequest
{
    public required string RoleDiscordId { get; set; }
    public bool CanWarn { get; set; }
    public bool CanViewWarnings { get; set; }
    public bool CanClearMessages { get; set; }
    public bool CanKick { get; set; }
    public bool CanViewModerationCases { get; set; }
    public bool CanViewLogs { get; set; }
}

public sealed class EvaluateModerationPermissionsResponse
{
    public bool CanWarn { get; init; }
    public bool CanViewWarnings { get; init; }
    public bool CanClearMessages { get; init; }
    public bool CanKick { get; init; }
    public bool CanViewModerationCases { get; init; }
    public bool CanViewLogs { get; init; }
    public bool CanAccessModeration { get; init; }
}
