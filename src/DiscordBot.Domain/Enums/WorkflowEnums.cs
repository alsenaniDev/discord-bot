namespace DiscordBot.Domain.Enums;

public enum WorkflowType { Application, Survey, Report, Custom }
public enum WorkflowStartMode { DirectMessage, Modal }
public enum WorkflowQuestionType { ShortText, LongText, Number, YesNo, SingleChoice }
public enum WorkflowSubmissionStatus { Pending, Approved, Rejected, Cancelled }
public enum WorkflowDuplicatePolicy { AllowMultiple, BlockWhilePending, BlockAfterApproved, CooldownAfterRejected, OneSubmissionEver }
public enum WorkflowApprovalActionType { AddRole, RemoveRole, SendDirectMessage }
public enum WorkflowPendingActionStatus { Pending, Succeeded, Failed }
