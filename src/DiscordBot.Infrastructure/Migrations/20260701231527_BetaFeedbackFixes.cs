using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BetaFeedbackFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActorUsername",
                table: "LogEntries",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChannelName",
                table: "LogEntries",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoleDiscordId",
                table: "LogEntries",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoleName",
                table: "LogEntries",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetUsername",
                table: "LogEntries",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommandPanelImageUrl",
                table: "GuildSettings",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TicketArchiveChannelId",
                table: "GuildSettings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommunityType",
                table: "Guilds",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Guilds",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "Guilds",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RulesUrl",
                table: "Guilds",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupportMessage",
                table: "Guilds",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebsiteUrl",
                table: "Guilds",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ModerationPermissionRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CanWarn = table.Column<bool>(type: "boolean", nullable: false),
                    CanViewWarnings = table.Column<bool>(type: "boolean", nullable: false),
                    CanClearMessages = table.Column<bool>(type: "boolean", nullable: false),
                    CanKick = table.Column<bool>(type: "boolean", nullable: false),
                    CanViewModerationCases = table.Column<bool>(type: "boolean", nullable: false),
                    CanViewLogs = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModerationPermissionRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModerationPermissionRoles_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModerationPermissionRoles_GuildId_RoleDiscordId",
                table: "ModerationPermissionRoles",
                columns: new[] { "GuildId", "RoleDiscordId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModerationPermissionRoles");

            migrationBuilder.DropColumn(
                name: "ActorUsername",
                table: "LogEntries");

            migrationBuilder.DropColumn(
                name: "ChannelName",
                table: "LogEntries");

            migrationBuilder.DropColumn(
                name: "RoleDiscordId",
                table: "LogEntries");

            migrationBuilder.DropColumn(
                name: "RoleName",
                table: "LogEntries");

            migrationBuilder.DropColumn(
                name: "TargetUsername",
                table: "LogEntries");

            migrationBuilder.DropColumn(
                name: "CommandPanelImageUrl",
                table: "GuildSettings");

            migrationBuilder.DropColumn(
                name: "TicketArchiveChannelId",
                table: "GuildSettings");

            migrationBuilder.DropColumn(
                name: "CommunityType",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "RulesUrl",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "SupportMessage",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "WebsiteUrl",
                table: "Guilds");
        }
    }
}
