namespace DiscordBot.Activities.Application.Models;

public sealed class ValidateGameAccessRequest
{
    public string DiscordGuildId { get; set; } = string.Empty;
    public string DiscordChannelId { get; set; } = string.Empty;
    public string DiscordUserId { get; set; } = string.Empty;
    public string GameKey { get; set; } = string.Empty;
}

public sealed class GameAccessResult
{
    public bool Allowed { get; set; }
    public string? DenialReason { get; set; }
    public string GameKey { get; set; } = string.Empty;
    public string GameVersion { get; set; } = string.Empty;
    public Guid? PlatformGameVersionId { get; set; }
    public string Mode { get; set; } = "Production";
    public string? ActivityRoute { get; set; }
    public bool SupportsWallet { get; set; }
    public RouletteSettingsSnapshot? RouletteSettings { get; set; }
}

public sealed class ReserveWalletRequest
{
    public string DiscordGuildId { get; set; } = string.Empty;
    public string DiscordUserId { get; set; } = string.Empty;
    public string GameKey { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "coins";
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class WalletReservationResult
{
    public bool Succeeded { get; set; }
    public string? ReservationId { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class WalletCreditRequest
{
    public string DiscordGuildId { get; set; } = string.Empty;
    public string DiscordUserId { get; set; } = string.Empty;
    public string GameKey { get; set; } = string.Empty;
    public Guid GameSessionId { get; set; }
    public Guid RoundId { get; set; }
    public Guid PayoutId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "coins";
    public string Reason { get; set; } = "roulette_payout";
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class WalletCreditResult
{
    public bool Succeeded { get; set; }
    public string? Status { get; set; }
    public decimal Amount { get; set; }
    public string? ErrorMessage { get; set; }
}
