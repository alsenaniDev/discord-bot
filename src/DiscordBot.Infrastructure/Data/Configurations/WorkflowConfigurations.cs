using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class GuildWorkflowConfiguration : IEntityTypeConfiguration<GuildWorkflow>
{
    public void Configure(EntityTypeBuilder<GuildWorkflow> b)
    {
        b.ToTable("GuildWorkflows"); b.HasKey(x => x.Id); b.HasIndex(x => x.GuildId);
        b.Property(x => x.Name).HasMaxLength(150).IsRequired(); b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(32); b.Property(x => x.StartMode).HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.DuplicatePolicy).HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.ConfirmationTitle).HasMaxLength(256); b.Property(x => x.ConfirmationMessage).HasMaxLength(2000);
        b.Property(x => x.ConfirmationConfirmButtonText).HasMaxLength(80); b.Property(x => x.ConfirmationCancelButtonText).HasMaxLength(80);
        b.Property(x => x.SuccessMessage).HasMaxLength(2000); b.Property(x => x.RejectionMessage).HasMaxLength(2000);
        b.HasMany(x => x.Questions).WithOne(x => x.Workflow).HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.ApprovalActions).WithOne(x => x.Workflow).HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Submissions).WithOne(x => x.Workflow).HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkflowQuestionConfiguration : IEntityTypeConfiguration<WorkflowQuestion>
{
    public void Configure(EntityTypeBuilder<WorkflowQuestion> b)
    {
        b.ToTable("WorkflowQuestions"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.WorkflowId, x.SortOrder });
        b.Property(x => x.Label).HasMaxLength(300).IsRequired(); b.Property(x => x.HelpText).HasMaxLength(1000);
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(32); b.Property(x => x.OptionsJson).HasMaxLength(4000); b.Property(x => x.Placeholder).HasMaxLength(300);
    }
}

public class WorkflowSubmissionConfiguration : IEntityTypeConfiguration<WorkflowSubmission>
{
    public void Configure(EntityTypeBuilder<WorkflowSubmission> b)
    {
        b.ToTable("WorkflowSubmissions"); b.HasKey(x => x.Id); b.HasIndex(x => x.WorkflowId); b.HasIndex(x => x.GuildId);
        b.HasIndex(x => x.UserDiscordId); b.HasIndex(x => x.Status);
        b.Property(x => x.UserDiscordId).HasMaxLength(32).IsRequired(); b.Property(x => x.UserDisplayName).HasMaxLength(256);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24); b.Property(x => x.AnswersJson).HasColumnType("jsonb").IsRequired();
        b.Property(x => x.ReviewedByDiscordUserId).HasMaxLength(32); b.Property(x => x.ReviewedByDisplayName).HasMaxLength(256);
        b.Property(x => x.ReviewNote).HasMaxLength(2000); b.Property(x => x.LastActionError).HasMaxLength(2000);
        b.HasOne(x => x.Guild).WithMany(x => x.WorkflowSubmissions).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkflowApprovalActionConfiguration : IEntityTypeConfiguration<WorkflowApprovalAction>
{
    public void Configure(EntityTypeBuilder<WorkflowApprovalAction> b)
    {
        b.ToTable("WorkflowApprovalActions"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.WorkflowId, x.SortOrder });
        b.Property(x => x.ActionType).HasConversion<string>().HasMaxLength(32); b.Property(x => x.RoleDiscordId).HasMaxLength(32); b.Property(x => x.MessageText).HasMaxLength(2000);
    }
}

public class WorkflowPendingActionConfiguration : IEntityTypeConfiguration<WorkflowPendingAction>
{
    public void Configure(EntityTypeBuilder<WorkflowPendingAction> b)
    {
        b.ToTable("WorkflowPendingActions"); b.HasKey(x => x.Id); b.HasIndex(x => x.Status); b.HasIndex(x => x.SubmissionId); b.HasIndex(x => x.GuildId);
        b.Property(x => x.UserDiscordId).HasMaxLength(32).IsRequired(); b.Property(x => x.ActionType).HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.RoleDiscordId).HasMaxLength(32); b.Property(x => x.MessageText).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24); b.Property(x => x.FailureReason).HasMaxLength(2000);
        b.HasOne(x => x.Submission).WithMany(x => x.PendingActions).HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Guild).WithMany(x => x.WorkflowPendingActions).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Restrict);
    }
}
