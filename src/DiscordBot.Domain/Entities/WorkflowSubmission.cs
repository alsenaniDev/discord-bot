using DiscordBot.Domain.Enums;

namespace DiscordBot.Domain.Entities;

public class WorkflowSubmission : BaseEntity
{
    public Guid WorkflowId { get; set; }
    public GuildWorkflow Workflow { get; set; } = null!;
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public string UserDiscordId { get; set; } = string.Empty;
    public string? UserDisplayName { get; set; }
    public WorkflowSubmissionStatus Status { get; set; } = WorkflowSubmissionStatus.Pending;
    public string AnswersJson { get; set; } = "[]";
    public DateTimeOffset SubmittedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? ReviewedByDiscordUserId { get; set; }
    public string? ReviewedByDisplayName { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public string? ReviewNote { get; set; }
    public string? LastActionError { get; set; }
    public ICollection<WorkflowPendingAction> PendingActions { get; set; } = [];
}
