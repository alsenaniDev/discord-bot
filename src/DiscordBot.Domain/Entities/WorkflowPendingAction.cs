using DiscordBot.Domain.Enums;

namespace DiscordBot.Domain.Entities;

public class WorkflowPendingAction : BaseEntity
{
    public Guid SubmissionId { get; set; }
    public WorkflowSubmission Submission { get; set; } = null!;
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public string UserDiscordId { get; set; } = string.Empty;
    public WorkflowApprovalActionType ActionType { get; set; }
    public string? RoleDiscordId { get; set; }
    public string? MessageText { get; set; }
    public WorkflowPendingActionStatus Status { get; set; } = WorkflowPendingActionStatus.Pending;
    public string? FailureReason { get; set; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
}
