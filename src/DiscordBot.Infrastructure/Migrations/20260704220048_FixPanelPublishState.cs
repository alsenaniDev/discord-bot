using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPanelPublishState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastPublishError",
                table: "GuildPanels",
                newName: "LastPublishFailureReason");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastPublishAttemptedAtUtc",
                table: "GuildPanels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LastPublishFailed",
                table: "GuildPanels",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastPublishAttemptedAtUtc",
                table: "GuildPanels");

            migrationBuilder.DropColumn(
                name: "LastPublishFailed",
                table: "GuildPanels");

            migrationBuilder.RenameColumn(
                name: "LastPublishFailureReason",
                table: "GuildPanels",
                newName: "LastPublishError");
        }
    }
}
