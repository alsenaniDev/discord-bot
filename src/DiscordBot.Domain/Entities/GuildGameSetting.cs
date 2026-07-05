namespace DiscordBot.Domain.Entities;

public class GuildGameSetting : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public Guid PlatformGameDefinitionId { get; set; }
    public PlatformGameDefinition PlatformGameDefinition { get; set; } = null!;
    public bool IsEnabledForGuild { get; set; }
    public bool PointsEnabled { get; set; } = true;
    public int PointsPerWin { get; set; }
    public int CooldownSeconds { get; set; }
    public int MaxPlaysPerDay { get; set; }
    public bool PublishResultAfterGame { get; set; } = true;
    public bool PublishLeaderboardAfterGame { get; set; }
    public bool PublishOnlyWins { get; set; }
}
