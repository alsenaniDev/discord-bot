namespace DiscordBot.Infrastructure.Models;

public sealed class GuildModuleDto
{
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public bool AllowedByPlan { get; init; }
    public bool EffectiveEnabled { get; init; }
}

public sealed class UpdateGuildModuleRequest
{
    public bool IsEnabled { get; set; }
}

public sealed class GuildModuleStatusDto
{
    public string Key { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public bool AllowedByPlan { get; init; }
    public bool EffectiveEnabled { get; init; }
}

public sealed class ModuleUpdateResult
{
    public GuildModuleDto? Module { get; init; }
    public string? ErrorCode { get; init; }
}
