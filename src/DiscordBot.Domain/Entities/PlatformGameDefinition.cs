using DiscordBot.Domain.Enums;

namespace DiscordBot.Domain.Entities;

public class PlatformGameDefinition : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public string ActivityRoute { get; set; } = string.Empty;
    public string RequiredPlan { get; set; } = "free";
    public GamePlayMode PlayMode { get; set; } = GamePlayMode.Solo;
    public bool IsEnabledGlobally { get; set; } = true;
    public int DefaultPointsPerWin { get; set; } = 10;
    public int DefaultCooldownSeconds { get; set; } = 30;
    public int DefaultMaxPlaysPerDay { get; set; } = 10;
    public bool SupportsScores { get; set; } = true;
    public bool SupportsLeaderboard { get; set; } = true;
    public bool SupportsResultPublishing { get; set; } = true;
    public ICollection<GuildGameSetting> GuildSettings { get; set; } = [];
    public ICollection<GameSession> Sessions { get; set; } = [];
    public ICollection<GameContent> Content { get; set; } = [];
    public ICollection<GameVersion> Versions { get; set; } = [];
}
