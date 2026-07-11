using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Activities.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRouletteAnnouncementsAndJoinIntents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnnouncementAttemptCount",
                table: "RouletteGameSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AnnouncementCreatedAtUtc",
                table: "RouletteGameSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnnouncementLastError",
                table: "RouletteGameSessions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AnnouncementNextAttemptAtUtc",
                table: "RouletteGameSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AnnouncementRequestedAtUtc",
                table: "RouletteGameSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnnouncementStatus",
                table: "RouletteGameSessions",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "NotRequested");

            migrationBuilder.AddColumn<string>(
                name: "DiscordAnnouncementChannelId",
                table: "RouletteGameSessions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscordAnnouncementMessageId",
                table: "RouletteGameSessions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RouletteJoinIntents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscordGuildId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DiscordChannelId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UserDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Username = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsumedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouletteJoinIntents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouletteJoinIntents_GameSessions_GameSessionId",
                        column: x => x.GameSessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RouletteGameSessions_AnnouncementStatus_AnnouncementNextAtt~",
                table: "RouletteGameSessions",
                columns: new[] { "AnnouncementStatus", "AnnouncementNextAttemptAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RouletteJoinIntents_GameSessionId_UserDiscordId_Status",
                table: "RouletteJoinIntents",
                columns: new[] { "GameSessionId", "UserDiscordId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RouletteJoinIntents_UserDiscordId_DiscordGuildId_DiscordCha~",
                table: "RouletteJoinIntents",
                columns: new[] { "UserDiscordId", "DiscordGuildId", "DiscordChannelId", "Status", "ExpiresAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RouletteJoinIntents");

            migrationBuilder.DropIndex(
                name: "IX_RouletteGameSessions_AnnouncementStatus_AnnouncementNextAtt~",
                table: "RouletteGameSessions");

            migrationBuilder.DropColumn(
                name: "AnnouncementAttemptCount",
                table: "RouletteGameSessions");

            migrationBuilder.DropColumn(
                name: "AnnouncementCreatedAtUtc",
                table: "RouletteGameSessions");

            migrationBuilder.DropColumn(
                name: "AnnouncementLastError",
                table: "RouletteGameSessions");

            migrationBuilder.DropColumn(
                name: "AnnouncementNextAttemptAtUtc",
                table: "RouletteGameSessions");

            migrationBuilder.DropColumn(
                name: "AnnouncementRequestedAtUtc",
                table: "RouletteGameSessions");

            migrationBuilder.DropColumn(
                name: "AnnouncementStatus",
                table: "RouletteGameSessions");

            migrationBuilder.DropColumn(
                name: "DiscordAnnouncementChannelId",
                table: "RouletteGameSessions");

            migrationBuilder.DropColumn(
                name: "DiscordAnnouncementMessageId",
                table: "RouletteGameSessions");
        }
    }
}
