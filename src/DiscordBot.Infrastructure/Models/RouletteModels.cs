namespace DiscordBot.Infrastructure.Models;

public class RouletteSettingsDto
{
    public Guid GuildId { get; set; }
    public int MinPlayers { get; set; } = 2;
    public int MaxPlayers { get; set; } = 6;
    public int WinnerCoins { get; set; } = 100;
    public int SecondPlaceCoins { get; set; } = 50;
    public int ParticipationCoins { get; set; } = 10;
    public int JoinWindowSeconds { get; set; } = 120;
    public int TurnSeconds { get; set; } = 30;
    public bool AnnounceRoomCreated { get; set; } = true;
    public bool AnnounceWinner { get; set; } = true;
    public List<RoulettePowerUpSettingDto> PowerUps { get; set; } = [];
}

public class UpdateRouletteSettingsRequest : RouletteSettingsDto { }
public class CreateRouletteRoomRequest { public string GuildDiscordId { get; set; } = string.Empty; public string ChannelDiscordId { get; set; } = string.Empty; }
public class PurchasePowerUpRequest { public string GuildDiscordId { get; set; } = string.Empty; public string PowerUpKey { get; set; } = string.Empty; }
public class UsePowerUpRequest { public string GuildDiscordId { get; set; } = string.Empty; public string ChannelDiscordId { get; set; } = string.Empty; public string PowerUpKey { get; set; } = string.Empty; }
public class PrepareRouletteJoinRequest { public string GuildDiscordId { get; set; } = string.Empty; public string ChannelDiscordId { get; set; } = string.Empty; public string UserDiscordId { get; set; } = string.Empty; public string Username { get; set; } = string.Empty; }
public class PrepareRouletteJoinResponse { public Guid JoinIntentId { get; set; } public DateTimeOffset ExpiresAt { get; set; } }
public class PendingRouletteIntentDto { public Guid RoomId { get; set; } }
public class MyActiveRouletteRoomDto { public bool HasRoom { get; set; } public Guid? RoomId { get; set; } public string? Status { get; set; } public bool IsHost { get; set; } }
public class GameWalletDto { public int Balance { get; set; } }

public class RoulettePowerUpSettingDto
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsEnabledForGuild { get; set; } = true;
    public int Price { get; set; }
    public int MaxUsesPerGame { get; set; } = 1;
}

public class PowerUpStoreDto
{
    public int Balance { get; set; }
    public List<PowerUpStoreItemDto> Items { get; set; } = [];
}

public class PowerUpStoreItemDto : RoulettePowerUpSettingDto
{
    public int OwnedQuantity { get; set; }
}

public class PurchasePowerUpResponse
{
    public int Balance { get; set; }
    public string PowerUpKey { get; set; } = string.Empty;
    public int OwnedQuantity { get; set; }
}

public class RoulettePlayerDto
{
    public string UserDiscordId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public bool IsHost { get; set; }
    public bool IsAlive { get; set; }
    public int Position { get; set; }
    public int Eliminations { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? EliminatedAt { get; set; }
}

public class RouletteRoomDto
{
    public Guid Id { get; set; }
    public string GuildDiscordId { get; set; } = string.Empty;
    public string ChannelDiscordId { get; set; } = string.Empty;
    public string HostUserDiscordId { get; set; } = string.Empty;
    public string HostUsername { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
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
    public string? PendingTargetUserDiscordId { get; set; }
    public string? PendingTargetUsername { get; set; }
    public string PendingActionStatus { get; set; } = "None";
    public DateTimeOffset? PendingActionExpiresAt { get; set; }
    public RouletteSpinResultInfoDto? LastSpinResult { get; set; }
    public List<RoulettePlayerDto> Players { get; set; } = [];
    public List<RouletteActionDto> Actions { get; set; } = [];
    public RoulettePlayerDto? Winner { get; set; }
}

public class RouletteSpinResultDto
{
    public RouletteRoomDto Room { get; set; } = new();
    public RoulettePlayerDto? EliminatedPlayer { get; set; }
    public RoulettePlayerDto? TargetPlayer { get; set; }
}

public class RouletteSpinResultInfoDto
{
    public string SpinnerUserDiscordId { get; set; } = string.Empty;
    public string SpinnerUsername { get; set; } = string.Empty;
    public string TargetUserDiscordId { get; set; } = string.Empty;
    public string TargetUsername { get; set; } = string.Empty;
    public string ResultType { get; set; } = "PendingElimination";
    public DateTimeOffset CreatedAt { get; set; }
}

public class RouletteActionDto
{
    public int RoundNumber { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string ActorUserDiscordId { get; set; } = string.Empty;
    public string? TargetUserDiscordId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public class PendingRoulettePublishActionDto
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public string DiscordGuildId { get; set; } = string.Empty;
    public string ChannelDiscordId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string HostUsername { get; set; } = string.Empty;
    public string WinnerUsername { get; set; } = string.Empty;
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public int PlayersCount { get; set; }
    public int JoinWindowSeconds { get; set; }
    public int WinnerCoins { get; set; }
    public int CurrentRound { get; set; }
}

public class AckRoulettePublishActionRequest
{
    public bool Success { get; set; }
    public string? MessageDiscordId { get; set; }
    public string? ErrorMessage { get; set; }
}
