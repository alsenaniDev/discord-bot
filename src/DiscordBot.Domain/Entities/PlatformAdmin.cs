namespace DiscordBot.Domain.Entities;

/// <summary>
/// Platform owner who can access the admin dashboard and API.
/// </summary>
public class PlatformAdmin : BaseEntity
{
    public required string DiscordUserId { get; set; }
}
