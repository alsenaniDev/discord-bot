namespace DiscordBot.Domain.Entities;

public class GameVersion : BaseEntity
{
    public Guid GameDefinitionId { get; set; }
    public PlatformGameDefinition GameDefinition { get; set; } = null!;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public string? FrontendUrl { get; set; }
    public string? BackendUrl { get; set; }
    public string? ActivityRoute { get; set; }
    public string ManifestJson { get; set; } = "{}";
    public string? Notes { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public ICollection<GameSandboxAccess> SandboxAccess { get; set; } = [];
    public ICollection<GameEvent> Events { get; set; } = [];
}
