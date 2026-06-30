namespace DiscordBot.Infrastructure.Models;

public sealed class RegisterGuildRequest
{
    public required string DiscordGuildId { get; set; }
    public required string Name { get; set; }
    public required string OwnerDiscordUserId { get; set; }
    public string? IconUrl { get; set; }
}

public sealed class RegisterGuildResponse
{
    public Guid Id { get; init; }
    public string DiscordGuildId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsNew { get; init; }
}
