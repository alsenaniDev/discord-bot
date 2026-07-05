using DiscordBot.Domain.Enums;

namespace DiscordBot.Domain.Entities;

public class WorkflowApprovalAction : BaseEntity
{
    public Guid WorkflowId { get; set; }
    public GuildWorkflow Workflow { get; set; } = null!;
    public int SortOrder { get; set; }
    public WorkflowApprovalActionType ActionType { get; set; }
    public string? RoleDiscordId { get; set; }
    public string? MessageText { get; set; }
    public bool IsEnabled { get; set; } = true;
}
