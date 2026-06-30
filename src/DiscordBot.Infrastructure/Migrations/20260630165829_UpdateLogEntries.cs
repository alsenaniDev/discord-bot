using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLogEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Action",
                table: "LogEntries");

            migrationBuilder.DropColumn(
                name: "Details",
                table: "LogEntries");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "LogEntries");

            migrationBuilder.RenameColumn(
                name: "ModeratorDiscordUserId",
                table: "LogEntries",
                newName: "ChannelDiscordId");

            migrationBuilder.AddColumn<string>(
                name: "ActorDiscordUserId",
                table: "LogEntries",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Message",
                table: "LogEntries",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "LogEntries",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "LogEntries",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_GuildId_Type_CreatedAt",
                table: "LogEntries",
                columns: new[] { "GuildId", "Type", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LogEntries_GuildId_Type_CreatedAt",
                table: "LogEntries");

            migrationBuilder.DropColumn(
                name: "ActorDiscordUserId",
                table: "LogEntries");

            migrationBuilder.DropColumn(
                name: "Message",
                table: "LogEntries");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "LogEntries");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "LogEntries");

            migrationBuilder.RenameColumn(
                name: "ChannelDiscordId",
                table: "LogEntries",
                newName: "ModeratorDiscordUserId");

            migrationBuilder.AddColumn<string>(
                name: "Action",
                table: "LogEntries",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Details",
                table: "LogEntries",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "LogEntries",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}
