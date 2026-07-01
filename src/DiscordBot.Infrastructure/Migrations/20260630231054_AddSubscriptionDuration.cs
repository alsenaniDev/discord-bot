using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionDuration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DurationMonths",
                table: "PlanUpgradeRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedRequestId",
                table: "GuildSubscriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "GuildSubscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                table: "GuildSubscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "GuildSubscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_GuildSubscriptions_ApprovedRequestId",
                table: "GuildSubscriptions",
                column: "ApprovedRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_GuildSubscriptions_PlanUpgradeRequests_ApprovedRequestId",
                table: "GuildSubscriptions",
                column: "ApprovedRequestId",
                principalTable: "PlanUpgradeRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GuildSubscriptions_PlanUpgradeRequests_ApprovedRequestId",
                table: "GuildSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_GuildSubscriptions_ApprovedRequestId",
                table: "GuildSubscriptions");

            migrationBuilder.DropColumn(
                name: "DurationMonths",
                table: "PlanUpgradeRequests");

            migrationBuilder.DropColumn(
                name: "ApprovedRequestId",
                table: "GuildSubscriptions");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "GuildSubscriptions");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "GuildSubscriptions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "GuildSubscriptions");
        }
    }
}
