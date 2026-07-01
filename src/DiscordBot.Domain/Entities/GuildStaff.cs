using DiscordBot.Domain.Enums;

namespace DiscordBot.Domain.Entities;

public class GuildStaff : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    public required string DiscordUserId { get; set; }

    public GuildStaffRole Role { get; set; } = GuildStaffRole.Moderator;

    public required string CreatedByDiscordUserId { get; set; }
}
