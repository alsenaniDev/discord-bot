using DiscordBot.Domain.Enums;

namespace DiscordBot.Infrastructure.Models;

public sealed class WorkflowDto
{
    public Guid Id { get; init; } public Guid GuildId { get; init; } public string Name { get; init; } = ""; public string? Description { get; init; }
    public WorkflowType Type { get; init; } public WorkflowStartMode StartMode { get; init; } public bool IsEnabled { get; init; }
    public bool RequireConfirmation { get; init; } public string? ConfirmationTitle { get; init; } public string? ConfirmationMessage { get; init; }
    public string? ConfirmationConfirmButtonText { get; init; } public string? ConfirmationCancelButtonText { get; init; }
    public WorkflowDuplicatePolicy DuplicatePolicy { get; init; } public int? CooldownHours { get; init; } public int? MaxSubmissionsPerUser { get; init; }
    public string? SuccessMessage { get; init; } public string? RejectionMessage { get; init; }
    public int PendingSubmissionsCount { get; init; } public DateTimeOffset CreatedAtUtc { get; init; } public DateTimeOffset UpdatedAtUtc { get; init; }
    public IReadOnlyList<WorkflowQuestionDto> Questions { get; init; } = []; public IReadOnlyList<WorkflowApprovalActionDto> ApprovalActions { get; init; } = [];
}
public sealed class WorkflowQuestionOptionDto { public string Label { get; init; } = ""; public string Value { get; init; } = ""; public int SortOrder { get; init; } }
public sealed class WorkflowQuestionDto { public Guid Id { get; init; } public int SortOrder { get; init; } public string Label { get; init; } = ""; public string? HelpText { get; init; } public WorkflowQuestionType Type { get; init; } public bool IsRequired { get; init; } public int? MinLength { get; init; } public int? MaxLength { get; init; } public string? Placeholder { get; init; } public IReadOnlyList<WorkflowQuestionOptionDto> Options { get; init; } = []; }
public sealed class WorkflowApprovalActionDto { public Guid Id { get; init; } public int SortOrder { get; init; } public WorkflowApprovalActionType ActionType { get; init; } public string? RoleDiscordId { get; init; } public string? MessageText { get; init; } public bool IsEnabled { get; init; } }
public sealed class SaveWorkflowRequest
{
    public string Name { get; set; } = ""; public string? Description { get; set; } public WorkflowType Type { get; set; }
    public WorkflowStartMode StartMode { get; set; } = WorkflowStartMode.DirectMessage; public bool IsEnabled { get; set; } = true;
    public bool RequireConfirmation { get; set; } public string? ConfirmationTitle { get; set; } public string? ConfirmationMessage { get; set; }
    public string? ConfirmationConfirmButtonText { get; set; } public string? ConfirmationCancelButtonText { get; set; }
    public WorkflowDuplicatePolicy DuplicatePolicy { get; set; } public int? CooldownHours { get; set; } public int? MaxSubmissionsPerUser { get; set; }
    public string? SuccessMessage { get; set; } public string? RejectionMessage { get; set; }
    public List<SaveWorkflowQuestionRequest> Questions { get; set; } = []; public List<SaveWorkflowApprovalActionRequest> ApprovalActions { get; set; } = [];
}
public sealed class SaveWorkflowQuestionRequest { public Guid? Id { get; set; } public int SortOrder { get; set; } public string Label { get; set; } = ""; public string? HelpText { get; set; } public WorkflowQuestionType Type { get; set; } public bool IsRequired { get; set; } = true; public int? MinLength { get; set; } public int? MaxLength { get; set; } public string? Placeholder { get; set; } public List<WorkflowQuestionOptionDto> Options { get; set; } = []; }
public sealed class SaveWorkflowApprovalActionRequest { public Guid? Id { get; set; } public int SortOrder { get; set; } public WorkflowApprovalActionType ActionType { get; set; } public string? RoleDiscordId { get; set; } public string? MessageText { get; set; } public bool IsEnabled { get; set; } = true; }
public sealed class WorkflowAnswerDto { public Guid QuestionId { get; set; } public string Label { get; set; } = ""; public string Value { get; set; } = ""; public string? DisplayValue { get; set; } public string? QuestionType { get; set; } }
public sealed class WorkflowSubmissionDto
{
    public Guid Id { get; init; } public Guid WorkflowId { get; init; } public string WorkflowName { get; init; } = ""; public Guid GuildId { get; init; }
    public string UserDiscordId { get; init; } = ""; public string? UserDisplayName { get; init; } public WorkflowSubmissionStatus Status { get; init; }
    public IReadOnlyList<WorkflowAnswerDto> Answers { get; init; } = []; public DateTimeOffset SubmittedAtUtc { get; init; }
    public string? ReviewedByDiscordUserId { get; init; } public string? ReviewedByDisplayName { get; init; } public DateTimeOffset? ReviewedAtUtc { get; init; }
    public string? ReviewNote { get; init; } public string? LastActionError { get; init; }
}
public sealed class ReviewWorkflowSubmissionRequest { public string? ReviewNote { get; set; } public string? ReviewerDisplayName { get; set; } }
public sealed class WorkflowStartContextDto
{
    public bool CanStart { get; init; } public string? BlockReason { get; init; } public Guid WorkflowId { get; init; } public string Name { get; init; } = "";
    public bool RequireConfirmation { get; init; } public string? ConfirmationTitle { get; init; } public string? ConfirmationMessage { get; init; }
    public string ConfirmButtonText { get; init; } = "Continue"; public string CancelButtonText { get; init; } = "Cancel";
    public IReadOnlyList<WorkflowQuestionDto> Questions { get; init; } = []; public string? SuccessMessage { get; init; }
}
public sealed class CreateWorkflowSubmissionRequest { public string DiscordGuildId { get; set; } = ""; public string UserDiscordId { get; set; } = ""; public string? UserDisplayName { get; set; } public List<WorkflowAnswerDto> Answers { get; set; } = []; }
public sealed class CreateWorkflowSubmissionResult { public Guid SubmissionId { get; init; } public WorkflowSubmissionStatus Status { get; init; } public string Message { get; init; } = ""; }
public sealed class WorkflowPendingActionDto { public Guid Id { get; init; } public Guid SubmissionId { get; init; } public string DiscordGuildId { get; init; } = ""; public string UserDiscordId { get; init; } = ""; public WorkflowApprovalActionType ActionType { get; init; } public string? RoleDiscordId { get; init; } public string? MessageText { get; init; } }
public sealed class AckWorkflowPendingActionRequest { public bool Success { get; set; } public string? FailureReason { get; set; } }
