namespace DiscordBot.Infrastructure.Models;

public sealed class GuildProfileDto
{
    public Guid GuildId { get; init; }
    public string DiscordGuildId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? IconUrl { get; init; }
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public string? CommunityType { get; init; }
    public string? SupportMessage { get; init; }
    public string? RulesUrl { get; init; }
    public string? WebsiteUrl { get; init; }
}

public sealed class UpdateGuildProfileRequest
{
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? CommunityType { get; set; }
    public string? SupportMessage { get; set; }
    public string? RulesUrl { get; set; }
    public string? WebsiteUrl { get; set; }
}
