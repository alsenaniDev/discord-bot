using System.Text.Json;
using DiscordBot.Domain.Entities;
using DiscordBot.Domain.Enums;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Infrastructure.Services;

public interface IWorkflowService
{
    Task<IReadOnlyList<WorkflowDto>> ListAsync(Guid guildId, CancellationToken ct = default);
    Task<WorkflowDto?> GetAsync(Guid guildId, Guid workflowId, CancellationToken ct = default);
    Task<(WorkflowDto? Value, string? Error)> CreateAsync(Guid guildId, SaveWorkflowRequest request, CancellationToken ct = default);
    Task<(WorkflowDto? Value, string? Error)> UpdateAsync(Guid guildId, Guid workflowId, SaveWorkflowRequest request, CancellationToken ct = default);
    Task<(bool Found, string? Error)> DeleteAsync(Guid guildId, Guid workflowId, CancellationToken ct = default);
    Task<IReadOnlyList<WorkflowSubmissionDto>> ListSubmissionsAsync(Guid guildId, WorkflowSubmissionStatus? status, Guid? workflowId, CancellationToken ct = default);
    Task<WorkflowSubmissionDto?> GetSubmissionAsync(Guid guildId, Guid submissionId, CancellationToken ct = default);
    Task<(WorkflowSubmissionDto? Value, string? Error)> ReviewAsync(Guid guildId, Guid submissionId, bool approve, string reviewerId, ReviewWorkflowSubmissionRequest request, CancellationToken ct = default);
    Task<WorkflowStartContextDto?> GetStartContextAsync(Guid workflowId, string discordGuildId, string discordUserId, CancellationToken ct = default);
    Task<(CreateWorkflowSubmissionResult? Value, string? Error)> SubmitAsync(Guid workflowId, CreateWorkflowSubmissionRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<WorkflowPendingActionDto>> GetPendingActionsAsync(CancellationToken ct = default);
    Task<bool> AckPendingActionAsync(Guid actionId, AckWorkflowPendingActionRequest request, CancellationToken ct = default);
}

public class WorkflowService : IWorkflowService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _db;
    private readonly ILogger<WorkflowService> _logger;
    public WorkflowService(AppDbContext db, ILogger<WorkflowService> logger) { _db = db; _logger = logger; }

    public async Task<IReadOnlyList<WorkflowDto>> ListAsync(Guid guildId, CancellationToken ct = default)
    {
        var items = await BaseQuery().AsNoTracking().Where(x => x.GuildId == guildId).OrderBy(x => x.Name).ToListAsync(ct);
        return items.Select(Map).ToList();
    }
    public async Task<WorkflowDto?> GetAsync(Guid guildId, Guid workflowId, CancellationToken ct = default)
    {
        var item = await BaseQuery().AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == guildId && x.Id == workflowId, ct);
        return item is null ? null : Map(item);
    }
    public async Task<(WorkflowDto? Value, string? Error)> CreateAsync(Guid guildId, SaveWorkflowRequest request, CancellationToken ct = default)
    {
        var error = await ValidateAsync(guildId, request, ct); if (error is not null) return (null, error);
        var workflow = new GuildWorkflow { GuildId = guildId }; Apply(workflow, request); _db.GuildWorkflows.Add(workflow);
        await _db.SaveChangesAsync(ct); return (Map(workflow), null);
    }
    public async Task<(WorkflowDto? Value, string? Error)> UpdateAsync(Guid guildId, Guid workflowId, SaveWorkflowRequest request, CancellationToken ct = default)
    {
        var workflow = await BaseQuery().FirstOrDefaultAsync(x => x.GuildId == guildId && x.Id == workflowId, ct); if (workflow is null) return (null, null);
        var error = await ValidateAsync(guildId, request, ct); if (error is not null) return (null, error);
        Apply(workflow, request); await _db.SaveChangesAsync(ct); return (Map(workflow), null);
    }
    public async Task<(bool Found, string? Error)> DeleteAsync(Guid guildId, Guid workflowId, CancellationToken ct = default)
    {
        var workflow = await _db.GuildWorkflows.FirstOrDefaultAsync(x => x.GuildId == guildId && x.Id == workflowId, ct); if (workflow is null) return (false, null);
        if (await _db.WorkflowSubmissions.AnyAsync(x => x.WorkflowId == workflowId, ct)) return (true, "Workflows with submissions cannot be deleted. Disable the workflow instead.");
        _db.GuildWorkflows.Remove(workflow); await _db.SaveChangesAsync(ct); return (true, null);
    }
    public async Task<IReadOnlyList<WorkflowSubmissionDto>> ListSubmissionsAsync(Guid guildId, WorkflowSubmissionStatus? status, Guid? workflowId, CancellationToken ct = default)
    {
        var query = _db.WorkflowSubmissions.AsNoTracking().Include(x => x.Workflow).Where(x => x.GuildId == guildId);
        if (status.HasValue) query = query.Where(x => x.Status == status); if (workflowId.HasValue) query = query.Where(x => x.WorkflowId == workflowId);
        return (await query.OrderByDescending(x => x.SubmittedAtUtc).Take(250).ToListAsync(ct)).Select(MapSubmission).ToList();
    }
    public async Task<WorkflowSubmissionDto?> GetSubmissionAsync(Guid guildId, Guid submissionId, CancellationToken ct = default)
    {
        var item = await _db.WorkflowSubmissions.AsNoTracking().Include(x => x.Workflow).FirstOrDefaultAsync(x => x.GuildId == guildId && x.Id == submissionId, ct);
        return item is null ? null : MapSubmission(item);
    }
    public async Task<(WorkflowSubmissionDto? Value, string? Error)> ReviewAsync(Guid guildId, Guid submissionId, bool approve, string reviewerId, ReviewWorkflowSubmissionRequest request, CancellationToken ct = default)
    {
        var submission = await _db.WorkflowSubmissions.Include(x => x.Workflow).ThenInclude(x => x.ApprovalActions)
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.Id == submissionId, ct);
        if (submission is null) return (null, null); if (submission.Status != WorkflowSubmissionStatus.Pending) return (null, "Only pending submissions can be reviewed.");
        submission.Status = approve ? WorkflowSubmissionStatus.Approved : WorkflowSubmissionStatus.Rejected;
        submission.ReviewedByDiscordUserId = reviewerId; submission.ReviewedByDisplayName = Clean(request.ReviewerDisplayName);
        submission.ReviewNote = Clean(request.ReviewNote); submission.ReviewedAtUtc = DateTimeOffset.UtcNow; submission.LastActionError = null;
        IEnumerable<WorkflowApprovalAction> actions = approve
            ? submission.Workflow.ApprovalActions.Where(x => x.IsEnabled).OrderBy(x => x.SortOrder)
            : string.IsNullOrWhiteSpace(submission.Workflow.RejectionMessage)
                ? Array.Empty<WorkflowApprovalAction>()
                : [new WorkflowApprovalAction { ActionType = WorkflowApprovalActionType.SendDirectMessage, MessageText = submission.Workflow.RejectionMessage }];
        foreach (var action in actions) _db.WorkflowPendingActions.Add(new WorkflowPendingAction
        {
            Submission = submission, GuildId = guildId, UserDiscordId = submission.UserDiscordId, ActionType = action.ActionType,
            RoleDiscordId = action.RoleDiscordId, MessageText = action.MessageText
        });
        await _db.SaveChangesAsync(ct); return (MapSubmission(submission), null);
    }
    public async Task<WorkflowStartContextDto?> GetStartContextAsync(Guid workflowId, string discordGuildId, string discordUserId, CancellationToken ct = default)
    {
        var workflow = await BaseQuery().AsNoTracking().FirstOrDefaultAsync(x => x.Id == workflowId && x.Guild.DiscordGuildId == discordGuildId, ct);
        if (workflow is null) return null;
        var reason = !workflow.IsEnabled ? "This workflow is currently disabled." : await GetBlockReasonAsync(workflow, discordUserId, ct);
        return new WorkflowStartContextDto
        {
            CanStart = reason is null, BlockReason = reason, WorkflowId = workflow.Id, Name = workflow.Name,
            RequireConfirmation = workflow.RequireConfirmation, ConfirmationTitle = workflow.ConfirmationTitle, ConfirmationMessage = workflow.ConfirmationMessage,
            ConfirmButtonText = Clean(workflow.ConfirmationConfirmButtonText) ?? "Continue", CancelButtonText = Clean(workflow.ConfirmationCancelButtonText) ?? "Cancel",
            Questions = workflow.Questions.OrderBy(x => x.SortOrder).Select(MapQuestion).ToList(), SuccessMessage = workflow.SuccessMessage
        };
    }
    public async Task<(CreateWorkflowSubmissionResult? Value, string? Error)> SubmitAsync(Guid workflowId, CreateWorkflowSubmissionRequest request, CancellationToken ct = default)
    {
        var workflow = await BaseQuery().FirstOrDefaultAsync(x => x.Id == workflowId && x.Guild.DiscordGuildId == request.DiscordGuildId, ct);
        if (workflow is null || !workflow.IsEnabled) return (null, "Workflow not found or disabled.");
        var blocked = await GetBlockReasonAsync(workflow, request.UserDiscordId, ct); if (blocked is not null) return (null, blocked);
        var normalized = new List<WorkflowAnswerDto>();
        foreach (var question in workflow.Questions.OrderBy(x => x.SortOrder))
        {
            var answer = request.Answers.FirstOrDefault(x => x.QuestionId == question.Id)?.Value?.Trim() ?? "";
            string? displayValue = null;
            if (question.Type == WorkflowQuestionType.YesNo && answer.Length > 0)
            {
                answer = NormalizeYesNo(answer) ?? "";
                if (answer.Length == 0) return (null, $"A yes or no answer is required for: {question.Label}");
            }
            if (question.Type == WorkflowQuestionType.SingleChoice && answer.Length > 0)
            {
                var option = ReadOptions(question.OptionsJson).FirstOrDefault(x => x.Value == answer);
                if (option is null) return (null, $"Select a valid option for: {question.Label}");
                displayValue = option.Label;
            }
            if (question.IsRequired && answer.Length == 0) return (null, $"An answer is required for: {question.Label}");
            if (question.Type is WorkflowQuestionType.ShortText or WorkflowQuestionType.LongText && question.MinLength.HasValue && answer.Length < question.MinLength) return (null, $"Answer is too short for: {question.Label}");
            if (question.Type is WorkflowQuestionType.ShortText or WorkflowQuestionType.LongText && question.MaxLength.HasValue && answer.Length > question.MaxLength) return (null, $"Answer is too long for: {question.Label}");
            if (question.Type == WorkflowQuestionType.Number && answer.Length > 0 && !decimal.TryParse(answer, out _)) return (null, $"A numeric answer is required for: {question.Label}");
            normalized.Add(new WorkflowAnswerDto { QuestionId = question.Id, Label = question.Label, Value = answer, DisplayValue = displayValue, QuestionType = question.Type.ToString() });
        }
        var submission = new WorkflowSubmission { WorkflowId = workflow.Id, GuildId = workflow.GuildId, UserDiscordId = request.UserDiscordId.Trim(), UserDisplayName = Clean(request.UserDisplayName), AnswersJson = JsonSerializer.Serialize(normalized, JsonOptions) };
        _db.WorkflowSubmissions.Add(submission); await _db.SaveChangesAsync(ct);
        return (new CreateWorkflowSubmissionResult { SubmissionId = submission.Id, Status = submission.Status, Message = Clean(workflow.SuccessMessage) ?? "Your submission has been received." }, null);
    }
    public async Task<IReadOnlyList<WorkflowPendingActionDto>> GetPendingActionsAsync(CancellationToken ct = default) =>
        await _db.WorkflowPendingActions.AsNoTracking().Where(x => x.Status == WorkflowPendingActionStatus.Pending)
            .OrderBy(x => x.CreatedAt).Take(100).Select(x => new WorkflowPendingActionDto
            { Id = x.Id, SubmissionId = x.SubmissionId, DiscordGuildId = x.Guild.DiscordGuildId, UserDiscordId = x.UserDiscordId, ActionType = x.ActionType, RoleDiscordId = x.RoleDiscordId, MessageText = x.MessageText }).ToListAsync(ct);
    public async Task<bool> AckPendingActionAsync(Guid actionId, AckWorkflowPendingActionRequest request, CancellationToken ct = default)
    {
        var action = await _db.WorkflowPendingActions.Include(x => x.Submission).FirstOrDefaultAsync(x => x.Id == actionId, ct); if (action is null) return false;
        action.Status = request.Success ? WorkflowPendingActionStatus.Succeeded : WorkflowPendingActionStatus.Failed;
        action.FailureReason = request.Success ? null : Clean(request.FailureReason) ?? "Action failed without a reason."; action.ProcessedAtUtc = DateTimeOffset.UtcNow;
        if (!request.Success) action.Submission.LastActionError = action.FailureReason;
        await _db.SaveChangesAsync(ct); return true;
    }

    private IQueryable<GuildWorkflow> BaseQuery() => _db.GuildWorkflows.Include(x => x.Guild).Include(x => x.Questions).Include(x => x.ApprovalActions).Include(x => x.Submissions);
    private async Task<string?> ValidateAsync(Guid guildId, SaveWorkflowRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return "Workflow name is required.";
        if (request.StartMode != WorkflowStartMode.DirectMessage) return "Only Direct Message workflows are supported in Phase 1.";
        if (request.IsEnabled && request.Questions.Count == 0) return "Enabled workflows require at least one question.";
        if (request.Questions.Count > 10) return "A workflow can contain at most 10 questions.";
        if (request.ApprovalActions.Count > 10) return "A workflow can contain at most 10 approval actions.";
        if (request.Questions.Any(x => string.IsNullOrWhiteSpace(x.Label))) return "Every question requires a label.";
        foreach (var question in request.Questions.Where(x => x.Type == WorkflowQuestionType.SingleChoice))
        {
            if (question.Options.Count is < 2 or > 5) return "Single-choice questions require between 2 and 5 options.";
            if (question.Options.Any(x => string.IsNullOrWhiteSpace(x.Label) || string.IsNullOrWhiteSpace(x.Value))) return "Every single-choice option requires a label and value.";
            if (question.Options.Any(x => x.Label.Trim().Length > 80)) return "Single-choice option labels cannot exceed 80 characters.";
            if (question.Options.Select(x => x.Value.Trim()).Distinct(StringComparer.Ordinal).Count() != question.Options.Count) return "Single-choice option values must be unique.";
        }
        if (request.RequireConfirmation && (string.IsNullOrWhiteSpace(request.ConfirmationTitle) || string.IsNullOrWhiteSpace(request.ConfirmationMessage) || string.IsNullOrWhiteSpace(request.ConfirmationConfirmButtonText) || string.IsNullOrWhiteSpace(request.ConfirmationCancelButtonText))) return "Confirmation title, message, and button text are required.";
        if (request.DuplicatePolicy == WorkflowDuplicatePolicy.CooldownAfterRejected && (!request.CooldownHours.HasValue || request.CooldownHours <= 0)) return "Cooldown hours are required for the rejected-submission cooldown policy.";
        foreach (var action in request.ApprovalActions.Where(x => x.IsEnabled))
        {
            if (action.ActionType is WorkflowApprovalActionType.AddRole or WorkflowApprovalActionType.RemoveRole)
            {
                if (string.IsNullOrWhiteSpace(action.RoleDiscordId)) return "Role actions require a Discord role.";
                if (!await _db.DiscordRoles.AnyAsync(x => x.GuildId == guildId && x.DiscordRoleId == action.RoleDiscordId && !x.IsManaged, ct)) return "The selected approval role is unavailable.";
            }
            if (action.ActionType == WorkflowApprovalActionType.SendDirectMessage && string.IsNullOrWhiteSpace(action.MessageText)) return "Direct message actions require message text.";
        }
        return null;
    }
    private async Task<string?> GetBlockReasonAsync(GuildWorkflow workflow, string userId, CancellationToken ct)
    {
        var submissions = await _db.WorkflowSubmissions.AsNoTracking().Where(x => x.WorkflowId == workflow.Id && x.UserDiscordId == userId).OrderByDescending(x => x.SubmittedAtUtc).ToListAsync(ct);
        if (workflow.MaxSubmissionsPerUser.HasValue && submissions.Count >= workflow.MaxSubmissionsPerUser) return "You have reached the maximum number of submissions.";
        return workflow.DuplicatePolicy switch
        {
            WorkflowDuplicatePolicy.BlockWhilePending when submissions.Any(x => x.Status == WorkflowSubmissionStatus.Pending) => "You already have a pending submission.",
            WorkflowDuplicatePolicy.BlockAfterApproved when submissions.Any(x => x.Status == WorkflowSubmissionStatus.Approved) => "You already have an approved submission.",
            WorkflowDuplicatePolicy.OneSubmissionEver when submissions.Count > 0 => "You have already submitted this workflow.",
            WorkflowDuplicatePolicy.CooldownAfterRejected when submissions.FirstOrDefault()?.Status == WorkflowSubmissionStatus.Rejected && submissions[0].ReviewedAtUtc > DateTimeOffset.UtcNow.AddHours(-(workflow.CooldownHours ?? 0)) => "Please wait before submitting again.",
            _ => null
        };
    }
    private static void Apply(GuildWorkflow w, SaveWorkflowRequest r)
    {
        w.Name = r.Name.Trim(); w.Description = Clean(r.Description); w.Type = r.Type; w.StartMode = r.StartMode; w.IsEnabled = r.IsEnabled;
        w.RequireConfirmation = r.RequireConfirmation; w.ConfirmationTitle = Clean(r.ConfirmationTitle); w.ConfirmationMessage = Clean(r.ConfirmationMessage);
        w.ConfirmationConfirmButtonText = Clean(r.ConfirmationConfirmButtonText); w.ConfirmationCancelButtonText = Clean(r.ConfirmationCancelButtonText);
        w.DuplicatePolicy = r.DuplicatePolicy; w.CooldownHours = r.CooldownHours; w.MaxSubmissionsPerUser = r.MaxSubmissionsPerUser;
        w.SuccessMessage = Clean(r.SuccessMessage); w.RejectionMessage = Clean(r.RejectionMessage);
        ReconcileQuestions(w, r.Questions); ReconcileActions(w, r.ApprovalActions);
    }
    private static void ReconcileQuestions(GuildWorkflow w, List<SaveWorkflowQuestionRequest> items)
    {
        var ids = items.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet(); foreach (var old in w.Questions.Where(x => !ids.Contains(x.Id)).ToList()) w.Questions.Remove(old);
        foreach (var i in items) { var q = i.Id.HasValue ? w.Questions.FirstOrDefault(x => x.Id == i.Id) : null; if (q is null) { q = new WorkflowQuestion(); w.Questions.Add(q); }
            q.SortOrder = i.SortOrder; q.Label = i.Label.Trim(); q.HelpText = Clean(i.HelpText); q.Type = i.Type; q.IsRequired = i.IsRequired; q.MinLength = i.MinLength;
            q.MaxLength = i.MaxLength ?? (i.Type == WorkflowQuestionType.LongText ? 1000 : i.Type == WorkflowQuestionType.ShortText ? 200 : null); q.Placeholder = Clean(i.Placeholder);
            q.OptionsJson = i.Type == WorkflowQuestionType.SingleChoice
                ? JsonSerializer.Serialize(i.Options.OrderBy(x => x.SortOrder).Select((x, index) => new WorkflowQuestionOptionDto { Label = x.Label.Trim(), Value = x.Value.Trim(), SortOrder = index }), JsonOptions)
                : null; }
    }
    private static void ReconcileActions(GuildWorkflow w, List<SaveWorkflowApprovalActionRequest> items)
    {
        var ids = items.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet(); foreach (var old in w.ApprovalActions.Where(x => !ids.Contains(x.Id)).ToList()) w.ApprovalActions.Remove(old);
        foreach (var i in items) { var a = i.Id.HasValue ? w.ApprovalActions.FirstOrDefault(x => x.Id == i.Id) : null; if (a is null) { a = new WorkflowApprovalAction(); w.ApprovalActions.Add(a); }
            a.SortOrder = i.SortOrder; a.ActionType = i.ActionType; a.RoleDiscordId = Clean(i.RoleDiscordId); a.MessageText = Clean(i.MessageText); a.IsEnabled = i.IsEnabled; }
    }
    private static WorkflowDto Map(GuildWorkflow w) => new() { Id = w.Id, GuildId = w.GuildId, Name = w.Name, Description = w.Description, Type = w.Type, StartMode = w.StartMode, IsEnabled = w.IsEnabled, RequireConfirmation = w.RequireConfirmation, ConfirmationTitle = w.ConfirmationTitle, ConfirmationMessage = w.ConfirmationMessage, ConfirmationConfirmButtonText = w.ConfirmationConfirmButtonText, ConfirmationCancelButtonText = w.ConfirmationCancelButtonText, DuplicatePolicy = w.DuplicatePolicy, CooldownHours = w.CooldownHours, MaxSubmissionsPerUser = w.MaxSubmissionsPerUser, SuccessMessage = w.SuccessMessage, RejectionMessage = w.RejectionMessage, PendingSubmissionsCount = w.Submissions.Count(x => x.Status == WorkflowSubmissionStatus.Pending), CreatedAtUtc = w.CreatedAt, UpdatedAtUtc = w.UpdatedAt, Questions = w.Questions.OrderBy(x => x.SortOrder).Select(MapQuestion).ToList(), ApprovalActions = w.ApprovalActions.OrderBy(x => x.SortOrder).Select(x => new WorkflowApprovalActionDto { Id = x.Id, SortOrder = x.SortOrder, ActionType = x.ActionType, RoleDiscordId = x.RoleDiscordId, MessageText = x.MessageText, IsEnabled = x.IsEnabled }).ToList() };
    private static WorkflowQuestionDto MapQuestion(WorkflowQuestion x) => new() { Id = x.Id, SortOrder = x.SortOrder, Label = x.Label, HelpText = x.HelpText, Type = x.Type, IsRequired = x.IsRequired, MinLength = x.MinLength, MaxLength = x.MaxLength, Placeholder = x.Placeholder, Options = ReadOptions(x.OptionsJson) };
    private static WorkflowSubmissionDto MapSubmission(WorkflowSubmission x) => new() { Id = x.Id, WorkflowId = x.WorkflowId, WorkflowName = x.Workflow.Name, GuildId = x.GuildId, UserDiscordId = x.UserDiscordId, UserDisplayName = x.UserDisplayName, Status = x.Status, Answers = JsonSerializer.Deserialize<List<WorkflowAnswerDto>>(x.AnswersJson, JsonOptions) ?? [], SubmittedAtUtc = x.SubmittedAtUtc, ReviewedByDiscordUserId = x.ReviewedByDiscordUserId, ReviewedByDisplayName = x.ReviewedByDisplayName, ReviewedAtUtc = x.ReviewedAtUtc, ReviewNote = x.ReviewNote, LastActionError = x.LastActionError };
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static IReadOnlyList<WorkflowQuestionOptionDto> ReadOptions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return (JsonSerializer.Deserialize<List<WorkflowQuestionOptionDto>>(json, JsonOptions) ?? []).OrderBy(x => x.SortOrder).ToList(); }
        catch (JsonException) { return []; }
    }
    private static string? NormalizeYesNo(string value) => value.Trim().ToLowerInvariant() switch
    {
        "yes" or "y" or "true" or "نعم" or "ايه" or "أيه" or "ايوه" or "أيوه" => "yes",
        "no" or "n" or "false" or "لا" => "no",
        _ => null
    };
}
