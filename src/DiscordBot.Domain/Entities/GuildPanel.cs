namespace DiscordBot.Domain.Entities;

public class GuildPanel : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string ChannelDiscordId { get; set; } = string.Empty;
    public string? MessageDiscordId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsPublished { get; set; }
    public bool RefreshRequested { get; set; }
    public DateTimeOffset? LastPublishedAtUtc { get; set; }
    public bool LastPublishFailed { get; set; }
    public string? LastPublishFailureReason { get; set; }
    public DateTimeOffset? LastPublishAttemptedAtUtc { get; set; }
    public ICollection<GuildPanelButton> Buttons { get; set; } = [];
}
