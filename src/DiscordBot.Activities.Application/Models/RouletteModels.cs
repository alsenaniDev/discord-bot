namespace DiscordBot.Activities.Application.Models;

public sealed class RouletteSettingsSnapshot
{
    public int MinPlayers { get; set; } = 2;
    public int MaxPlayers { get; set; } = 6;
    public int WinnerCoins { get; set; } = 100;
    public int SecondPlaceCoins { get; set; } = 50;
    public int ParticipationCoins { get; set; } = 10;
    public int JoinWindowSeconds { get; set; } = 120;
    public int TurnSeconds { get; set; } = 30;
}

public sealed class CreateRouletteSessionRequest
{
    public string GuildDiscordId { get; set; } = string.Empty;
    public string ChannelDiscordId { get; set; } = string.Empty;
    public string? ActivityInstanceId { get; set; }
    public string? IdempotencyKey { get; set; }
}

public class RouletteScopeRequest
{
    public string GuildDiscordId { get; set; } = string.Empty;
    public string ChannelDiscordId { get; set; } = string.Empty;
    public string? ActivityInstanceId { get; set; }
    public string? IdempotencyKey { get; set; }
}

public sealed class PlaceRouletteBetRequest : RouletteScopeRequest
{
    public string BetType { get; set; } = string.Empty;
    public string BetValue { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "coins";
}

public sealed class MyActiveRouletteSessionDto
{
    public bool HasRoom { get; set; }
    public Guid? RoomId { get; set; }
    public Guid? GameSessionId { get; set; }
    public string? Status { get; set; }
    public bool IsHost { get; set; }
}

public sealed class PendingRouletteIntentDto
{
    public Guid RoomId { get; set; }
    public Guid GameSessionId { get; set; }
}

public sealed class PrepareRouletteJoinRequest
{
    public string GuildDiscordId { get; set; } = string.Empty;
    public string ChannelDiscordId { get; set; } = string.Empty;
    public string UserDiscordId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}

public sealed class PrepareRouletteJoinResponse
{
    public Guid JoinIntentId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class PendingRouletteAnnouncementDto
{
    public Guid GameSessionId { get; set; }
    public string DiscordGuildId { get; set; } = string.Empty;
    public string DiscordChannelId { get; set; } = string.Empty;
    public string CreatedByDiscordUserId { get; set; } = string.Empty;
    public string CreatorUsername { get; set; } = string.Empty;
    public string GameKey { get; set; } = "roulette";
    public string ActivityRoute { get; set; } = "/games/roulette";
    public string Status { get; set; } = "Waiting";
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public int PlayersCount { get; set; }
    public int JoinWindowSeconds { get; set; }
    public int WinnerCoins { get; set; }
    public int AnnouncementAttemptCount { get; set; }
}

public sealed class AckRouletteAnnouncementRequest
{
    public bool Success { get; set; }
    public string? MessageDiscordId { get; set; }
    public string? ErrorMessage { get; set; }
    public int? RetryAfterSeconds { get; set; }
}

public sealed class RoulettePlayerDto
{
    public string UserDiscordId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsHost { get; set; }
    public bool IsAlive { get; set; } = true;
    public int Position { get; set; }
    public int Eliminations { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? EliminatedAt { get; set; }
}

public sealed class RouletteSessionDto
{
    public Guid Id { get; set; }
    public Guid GameSessionId { get; set; }
    public Guid ActivitySessionId { get; set; }
    public string GuildDiscordId { get; set; } = string.Empty;
    public string ChannelDiscordId { get; set; } = string.Empty;
    public string HostUserDiscordId { get; set; } = string.Empty;
    public string HostUsername { get; set; } = string.Empty;
    public string Status { get; set; } = "Waiting";
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public int WinnerCoins { get; set; }
    public int SecondPlaceCoins { get; set; }
    public int ParticipationCoins { get; set; }
    public int CurrentRound { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public bool CanStart { get; set; }
    public bool IsCurrentUserJoined { get; set; }
    public string? CurrentTurnUserDiscordId { get; set; }
    public string? CurrentTurnUsername { get; set; }
    public RoulettePlayerDto? CurrentTurnPlayer { get; set; }
    public string? PendingTargetUserDiscordId { get; set; }
    public string? PendingTargetUsername { get; set; }
    public RoulettePlayerDto? PendingTargetPlayer { get; set; }
    public string PendingActionStatus { get; set; } = "None";
    public DateTimeOffset? PendingActionExpiresAt { get; set; }
    public RouletteSpinResultInfoDto? LastSpinResult { get; set; }
    public List<RoulettePlayerDto> Players { get; set; } = [];
    public List<RoulettePlayerDto> AlivePlayers { get; set; } = [];
    public List<RoulettePlayerDto> EliminatedPlayers { get; set; } = [];
    public List<RouletteActionDto> Actions { get; set; } = [];
    public RoulettePlayerDto? Winner { get; set; }
}

public sealed class RouletteSpinResultDto
{
    public RouletteSessionDto Room { get; set; } = new();
    public RoulettePlayerDto? TargetPlayer { get; set; }
    public List<RoulettePlayerDto> AlivePlayers { get; set; } = [];
    public int SelectedIndex { get; set; }
    public bool TargetHasUsablePowerUps { get; set; }
    public List<object> UsablePowerUps { get; set; } = [];
}

public sealed class RouletteSpinResultInfoDto
{
    public string SpinnerUserDiscordId { get; set; } = string.Empty;
    public string SpinnerUsername { get; set; } = string.Empty;
    public string? SpinnerAvatarUrl { get; set; }
    public string TargetUserDiscordId { get; set; } = string.Empty;
    public string TargetUsername { get; set; } = string.Empty;
    public string? TargetAvatarUrl { get; set; }
    public int SelectedIndex { get; set; }
    public string ResultType { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class RouletteActionDto
{
    public int RoundNumber { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string ActorUserDiscordId { get; set; } = string.Empty;
    public string? TargetUserDiscordId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? PowerUpKey { get; set; }
    public string? PowerUpName { get; set; }
    public string? PowerUpIcon { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class RouletteBetDto
{
    public Guid Id { get; set; }
    public Guid RouletteRoundId { get; set; }
    public string DiscordUserId { get; set; } = string.Empty;
    public string BetType { get; set; } = string.Empty;
    public string BetValue { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "coins";
    public string Status { get; set; } = string.Empty;
}

public sealed class RouletteRealtimeEvent
{
    public Guid GameSessionId { get; set; }
    public string Type { get; set; } = string.Empty;
    public object Payload { get; set; } = new();
}
