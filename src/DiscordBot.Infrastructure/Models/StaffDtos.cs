using DiscordBot.Domain.Enums;

namespace DiscordBot.Infrastructure.Models;

public sealed class GuildAccessDto
{
    public bool IsOwner { get; init; }
    public bool IsPlatformAdmin { get; init; }
    public string? StaffRole { get; init; }
    public GuildPermissions Permissions { get; init; }
    public bool CanWarn { get; init; }
    public bool CanKick { get; init; }
    public bool CanTimeout { get; init; }
    public bool CanClearMessages { get; init; }
    public bool CanManageSettings { get; init; }
    public bool CanManageModules { get; init; }
    public bool CanManageSubscription { get; init; }
    public bool CanManageStaff { get; init; }
    public bool CanAccessModeration { get; init; }
    public bool CanAccessLogs { get; init; }
    public bool CanAccessTickets { get; init; }
    public bool CanViewTickets { get; init; }
    public bool CanReplyToTickets { get; init; }
    public bool CanCloseTickets { get; init; }
    public bool CanAccessOverview { get; init; }
    public bool CanClearLogs { get; init; }
}
