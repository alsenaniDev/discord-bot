using DiscordBot.Domain.Enums;

namespace DiscordBot.Domain.Entities;

public class GuildWorkflow : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkflowType Type { get; set; } = WorkflowType.Application;
    public WorkflowStartMode StartMode { get; set; } = WorkflowStartMode.DirectMessage;
    public bool IsEnabled { get; set; } = true;
    public bool RequireConfirmation { get; set; }
    public string? ConfirmationTitle { get; set; }
    public string? ConfirmationMessage { get; set; }
    public string? ConfirmationConfirmButtonText { get; set; }
    public string? ConfirmationCancelButtonText { get; set; }
    public WorkflowDuplicatePolicy DuplicatePolicy { get; set; } = WorkflowDuplicatePolicy.BlockWhilePending;
    public int? CooldownHours { get; set; }
    public int? MaxSubmissionsPerUser { get; set; }
    public string? SuccessMessage { get; set; }
    public string? RejectionMessage { get; set; }
    public ICollection<WorkflowQuestion> Questions { get; set; } = [];
    public ICollection<WorkflowSubmission> Submissions { get; set; } = [];
    public ICollection<WorkflowApprovalAction> ApprovalActions { get; set; } = [];
}
