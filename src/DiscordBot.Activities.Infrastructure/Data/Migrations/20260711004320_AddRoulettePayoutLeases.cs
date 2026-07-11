using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Activities.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRoulettePayoutLeases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoulettePayouts_Status_LastAttemptAtUtc",
                table: "RoulettePayouts");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextAttemptAtUtc",
                table: "RoulettePayouts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingOwner",
                table: "RoulettePayouts",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessingStartedAtUtc",
                table: "RoulettePayouts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoulettePayouts_Status_NextAttemptAtUtc",
                table: "RoulettePayouts",
                columns: new[] { "Status", "NextAttemptAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RoulettePayouts_Status_ProcessingStartedAtUtc",
                table: "RoulettePayouts",
                columns: new[] { "Status", "ProcessingStartedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoulettePayouts_Status_NextAttemptAtUtc",
                table: "RoulettePayouts");

            migrationBuilder.DropIndex(
                name: "IX_RoulettePayouts_Status_ProcessingStartedAtUtc",
                table: "RoulettePayouts");

            migrationBuilder.DropColumn(
                name: "NextAttemptAtUtc",
                table: "RoulettePayouts");

            migrationBuilder.DropColumn(
                name: "ProcessingOwner",
                table: "RoulettePayouts");

            migrationBuilder.DropColumn(
                name: "ProcessingStartedAtUtc",
                table: "RoulettePayouts");

            migrationBuilder.CreateIndex(
                name: "IX_RoulettePayouts_Status_LastAttemptAtUtc",
                table: "RoulettePayouts",
                columns: new[] { "Status", "LastAttemptAtUtc" });
        }
    }
}
