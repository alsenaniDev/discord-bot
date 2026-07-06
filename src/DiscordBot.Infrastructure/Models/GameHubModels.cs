using DiscordBot.Domain.Enums;

namespace DiscordBot.Infrastructure.Models;

public class PlatformGameDefinitionDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public string ActivityRoute { get; set; } = string.Empty;
    public string RequiredPlan { get; set; } = "free";
    public GamePlayMode PlayMode { get; set; } = GamePlayMode.Solo;
    public bool IsEnabledGlobally { get; set; }
    public int DefaultPointsPerWin { get; set; }
    public int DefaultCooldownSeconds { get; set; }
    public int DefaultMaxPlaysPerDay { get; set; }
    public bool SupportsScores { get; set; }
    public bool SupportsLeaderboard { get; set; }
    public bool SupportsResultPublishing { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class SavePlatformGameDefinitionRequest
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
}

public class GuildGamesSettingsDto
{
    public Guid GuildId { get; set; }
    public bool IsEnabled { get; set; }
    public string? GamesChannelDiscordId { get; set; }
    public bool AutoPostPanel { get; set; }
    public string? GamesPanelMessageDiscordId { get; set; }
}

public class UpdateGuildGamesSettingsRequest
{
    public bool IsEnabled { get; set; }
    public string? GamesChannelDiscordId { get; set; }
    public bool AutoPostPanel { get; set; }
}

public class GuildGameDto : PlatformGameDefinitionDto
{
    public bool IsAvailableByPlan { get; set; }
    public bool IsEnabledForGuild { get; set; }
    public bool PointsEnabled { get; set; }
    public int PointsPerWin { get; set; }
    public int CooldownSeconds { get; set; }
    public int MaxPlaysPerDay { get; set; }
    public bool PublishResultAfterGame { get; set; }
    public bool PublishLeaderboardAfterGame { get; set; }
    public bool PublishOnlyWins { get; set; }
    public string? LockedReason { get; set; }
}

public class UpdateGuildGameSettingRequest
{
    public bool IsEnabledForGuild { get; set; }
    public bool PointsEnabled { get; set; } = true;
    public int PointsPerWin { get; set; }
    public int CooldownSeconds { get; set; }
    public int MaxPlaysPerDay { get; set; }
    public bool PublishResultAfterGame { get; set; } = true;
    public bool PublishLeaderboardAfterGame { get; set; }
    public bool PublishOnlyWins { get; set; }
}

public class GameLeaderboardEntryDto
{
    public int Rank { get; set; }
    public string UserDiscordId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int TotalPoints { get; set; }
    public int GamesPlayed { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
}

public class StartGameSessionRequest
{
    public string GuildDiscordId { get; set; } = string.Empty;
    public string ChannelDiscordId { get; set; } = string.Empty;
    public string UserDiscordId { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string GameKey { get; set; } = string.Empty;
}

public class StartGameSessionResponse
{
    public Guid SessionId { get; set; }
    public string GameKey { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public string ActivityRoute { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}

public class CompleteGameSessionRequest
{
    public Guid SessionId { get; set; }
    public string GuildDiscordId { get; set; } = string.Empty;
    public string UserDiscordId { get; set; } = string.Empty;
    public int Score { get; set; }
    public bool Won { get; set; }
}

public class CompleteGameSessionResponse
{
    public Guid SessionId { get; set; }
    public int PointsAwarded { get; set; }
    public GameLeaderboardEntryDto Player { get; set; } = new();
}

public class AvailableGameDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public string ActivityRoute { get; set; } = string.Empty;
    public GamePlayMode PlayMode { get; set; } = GamePlayMode.Solo;
    public bool SupportsScores { get; set; }
    public bool SupportsLeaderboard { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public string RequiredPlanInternal { get; set; } = "free";
}

public class ActivityGamesContextDto
{
    public string GuildDiscordId { get; set; } = string.Empty;
    public string ChannelDiscordId { get; set; } = string.Empty;
    public string GamesChannelDiscordId { get; set; } = string.Empty;
    public List<AvailableGameDto> Games { get; set; } = [];
    public IReadOnlyList<GameLeaderboardEntryDto> Leaderboard { get; set; } = [];
}

public class ActivityStartGameSessionRequest
{
    public string GuildDiscordId { get; set; } = string.Empty;
    public string ChannelDiscordId { get; set; } = string.Empty;
    public string GameKey { get; set; } = string.Empty;
}

public class ActivityCompleteGameSessionRequest
{
    public Guid SessionId { get; set; }
    public string GuildDiscordId { get; set; } = string.Empty;
    public int Score { get; set; }
    public bool Won { get; set; }
}

public class BotGamesContextDto
{
    public bool GuildLinked { get; set; }
    public bool IsEnabled { get; set; }
    public string? GamesChannelDiscordId { get; set; }
    public List<AvailableGameDto> Games { get; set; } = [];
}

public class PendingGamePublishActionDto
{
    public Guid Id { get; set; }
    public Guid GameSessionId { get; set; }
    public string DiscordGuildId { get; set; } = string.Empty;
    public string ChannelDiscordId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class AckGamePublishActionRequest
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public record GameHubResult<T>(T? Value, string? Error, int StatusCode = 200)
{
    public bool Succeeded => Error is null;
    public static GameHubResult<T> Ok(T value) => new(value, null);
    public static GameHubResult<T> Fail(string error, int statusCode = 400) => new(default, error, statusCode);
}
