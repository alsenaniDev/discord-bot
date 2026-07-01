using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommandPanelAndTicketCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommandPanelButtonsJson",
                table: "GuildSettings",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "[{\"id\":\"ticket-open\",\"action\":\"ticket_open\",\"label\":\"Create Ticket\",\"style\":\"Success\",\"enabled\":true,\"order\":0},{\"id\":\"ticket-help\",\"action\":\"ticket_help\",\"label\":\"Ticket Help\",\"style\":\"Secondary\",\"enabled\":true,\"order\":1}]");

            migrationBuilder.AddColumn<string>(
                name: "CommandPanelChannelId",
                table: "GuildSettings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommandPanelDescription",
                table: "GuildSettings",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "Use the buttons below — no commands needed.");

            migrationBuilder.AddColumn<bool>(
                name: "CommandPanelEnabled",
                table: "GuildSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CommandPanelMessageId",
                table: "GuildSettings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CommandPanelRefreshRequested",
                table: "GuildSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CommandPanelTitle",
                table: "GuildSettings",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "How can we help?");

            migrationBuilder.AddColumn<bool>(
                name: "ChannelCleanupRequested",
                table: "Tickets",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommandPanelButtonsJson",
                table: "GuildSettings");

            migrationBuilder.DropColumn(
                name: "CommandPanelChannelId",
                table: "GuildSettings");

            migrationBuilder.DropColumn(
                name: "CommandPanelDescription",
                table: "GuildSettings");

            migrationBuilder.DropColumn(
                name: "CommandPanelEnabled",
                table: "GuildSettings");

            migrationBuilder.DropColumn(
                name: "CommandPanelMessageId",
                table: "GuildSettings");

            migrationBuilder.DropColumn(
                name: "CommandPanelRefreshRequested",
                table: "GuildSettings");

            migrationBuilder.DropColumn(
                name: "CommandPanelTitle",
                table: "GuildSettings");

            migrationBuilder.DropColumn(
                name: "ChannelCleanupRequested",
                table: "Tickets");
        }
    }
}
