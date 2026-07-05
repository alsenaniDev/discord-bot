using DiscordBot.Domain.Enums;

namespace DiscordBot.Domain.Entities;

public class WorkflowQuestion : BaseEntity
{
    public Guid WorkflowId { get; set; }
    public GuildWorkflow Workflow { get; set; } = null!;
    public int SortOrder { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? HelpText { get; set; }
    public WorkflowQuestionType Type { get; set; }
    public bool IsRequired { get; set; } = true;
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? OptionsJson { get; set; }
    public string? Placeholder { get; set; }
}
