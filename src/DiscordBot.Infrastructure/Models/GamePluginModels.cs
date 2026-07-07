namespace DiscordBot.Infrastructure.Models;

public class GameVersionDto
{
    public Guid Id { get; set; }
    public Guid GameDefinitionId { get; set; }
    public string GameKey { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? FrontendUrl { get; set; }
    public string? BackendUrl { get; set; }
    public string? ActivityRoute { get; set; }
    public string ManifestJson { get; set; } = "{}";
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public List<GameSandboxAccessDto> SandboxAccess { get; set; } = [];
}

public class CreateGameVersionRequest
{
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public string? FrontendUrl { get; set; }
    public string? BackendUrl { get; set; }
    public string? ActivityRoute { get; set; }
    public string ManifestJson { get; set; } = "{}";
    public string? Notes { get; set; }
}

public class UpdateGameVersionStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class GameSandboxAccessDto
{
    public Guid Id { get; set; }
    public Guid GameVersionId { get; set; }
    public string GuildDiscordId { get; set; } = string.Empty;
    public string? UserDiscordId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class AddGameSandboxAccessRequest
{
    public string GuildDiscordId { get; set; } = string.Empty;
    public string? UserDiscordId { get; set; }
}

public class IssueGameRuntimeTokenRequest
{
    public string GameKey { get; set; } = string.Empty;
    public string GuildDiscordId { get; set; } = string.Empty;
    public string ChannelDiscordId { get; set; } = string.Empty;
}

public class IssueGameRuntimeTokenResponse
{
    public string RuntimeToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public string Mode { get; set; } = "Production";
    public Guid GameVersionId { get; set; }
}

public class GameRuntimeContextDto
{
    public string GameKey { get; set; } = string.Empty;
    public Guid GameVersionId { get; set; }
    public Guid GuildId { get; set; }
    public string GuildDiscordId { get; set; } = string.Empty;
    public string ChannelDiscordId { get; set; } = string.Empty;
    public string UserDiscordId { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public string Mode { get; set; } = "Production";
}

public class GameIntegrationWalletDto
{
    public int Balance { get; set; }
}

public class RequestGameWalletTransactionRequest
{
    public int Amount { get; set; }
    public string Type { get; set; } = "game.debit";
    public string Reason { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class EmitGameEventRequest
{
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class GameEventDto
{
    public Guid Id { get; set; }
    public string GameKey { get; set; } = string.Empty;
    public Guid? GameVersionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public class RequestGameBotPublishRequest
{
    public string ChannelDiscordId { get; set; } = string.Empty;
    public string MessageJson { get; set; } = "{}";
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class GameBotPublishActionDto
{
    public Guid Id { get; set; }
    public Guid GameEventId { get; set; }
    public string ChannelDiscordId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string MessageJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
}

public class PendingGameBotPublishActionDto : GameBotPublishActionDto
{
    public string DiscordGuildId { get; set; } = string.Empty;
}

public class AckGameBotPublishActionRequest
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
