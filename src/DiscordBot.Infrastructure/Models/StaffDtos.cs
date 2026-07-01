using DiscordBot.Domain.Enums;

namespace DiscordBot.Infrastructure.Models;

public sealed class GuildStaffDto
{
    public Guid Id { get; init; }
    public Guid GuildId { get; init; }
    public string DiscordUserId { get; init; } = string.Empty;
    public GuildStaffRole Role { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string CreatedByDiscordUserId { get; init; } = string.Empty;
}

public sealed class AddGuildStaffRequest
{
    public required string DiscordUserId { get; set; }
    public GuildStaffRole Role { get; set; } = GuildStaffRole.Moderator;
}

public sealed class GuildAccessDto
{
    public bool IsOwner { get; init; }
    public bool IsPlatformAdmin { get; init; }
    public string? StaffRole { get; init; }
    public bool CanManageSettings { get; init; }
    public bool CanManageModules { get; init; }
    public bool CanManageSubscription { get; init; }
    public bool CanManageStaff { get; init; }
    public bool CanAccessModeration { get; init; }
    public bool CanAccessLogs { get; init; }
    public bool CanAccessTickets { get; init; }
    public bool CanAccessOverview { get; init; }
}
