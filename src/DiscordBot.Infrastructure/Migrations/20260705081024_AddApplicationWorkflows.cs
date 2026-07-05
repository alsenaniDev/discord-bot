using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowId",
                table: "GuildPanelButtons",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GuildWorkflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartMode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    RequireConfirmation = table.Column<bool>(type: "boolean", nullable: false),
                    ConfirmationTitle = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConfirmationMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ConfirmationConfirmButtonText = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ConfirmationCancelButtonText = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    DuplicatePolicy = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CooldownHours = table.Column<int>(type: "integer", nullable: true),
                    MaxSubmissionsPerUser = table.Column<int>(type: "integer", nullable: true),
                    SuccessMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RejectionMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildWorkflows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildWorkflows_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowApprovalActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RoleDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    MessageText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowApprovalActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowApprovalActions_GuildWorkflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "GuildWorkflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    HelpText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    MinLength = table.Column<int>(type: "integer", nullable: true),
                    MaxLength = table.Column<int>(type: "integer", nullable: true),
                    OptionsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Placeholder = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowQuestions_GuildWorkflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "GuildWorkflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UserDisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    AnswersJson = table.Column<string>(type: "jsonb", nullable: false),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedByDiscordUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ReviewedByDisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LastActionError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowSubmissions_GuildWorkflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "GuildWorkflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowSubmissions_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowPendingActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ActionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RoleDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    MessageText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowPendingActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowPendingActions_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowPendingActions_WorkflowSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "WorkflowSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuildPanelButtons_WorkflowId",
                table: "GuildPanelButtons",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildWorkflows_GuildId",
                table: "GuildWorkflows",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowApprovalActions_WorkflowId_SortOrder",
                table: "WorkflowApprovalActions",
                columns: new[] { "WorkflowId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowPendingActions_GuildId",
                table: "WorkflowPendingActions",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowPendingActions_Status",
                table: "WorkflowPendingActions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowPendingActions_SubmissionId",
                table: "WorkflowPendingActions",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowQuestions_WorkflowId_SortOrder",
                table: "WorkflowQuestions",
                columns: new[] { "WorkflowId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSubmissions_GuildId",
                table: "WorkflowSubmissions",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSubmissions_Status",
                table: "WorkflowSubmissions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSubmissions_UserDiscordId",
                table: "WorkflowSubmissions",
                column: "UserDiscordId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSubmissions_WorkflowId",
                table: "WorkflowSubmissions",
                column: "WorkflowId");

            migrationBuilder.AddForeignKey(
                name: "FK_GuildPanelButtons_GuildWorkflows_WorkflowId",
                table: "GuildPanelButtons",
                column: "WorkflowId",
                principalTable: "GuildWorkflows",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GuildPanelButtons_GuildWorkflows_WorkflowId",
                table: "GuildPanelButtons");

            migrationBuilder.DropTable(
                name: "WorkflowApprovalActions");

            migrationBuilder.DropTable(
                name: "WorkflowPendingActions");

            migrationBuilder.DropTable(
                name: "WorkflowQuestions");

            migrationBuilder.DropTable(
                name: "WorkflowSubmissions");

            migrationBuilder.DropTable(
                name: "GuildWorkflows");

            migrationBuilder.DropIndex(
                name: "IX_GuildPanelButtons_WorkflowId",
                table: "GuildPanelButtons");

            migrationBuilder.DropColumn(
                name: "WorkflowId",
                table: "GuildPanelButtons");
        }
    }
}
