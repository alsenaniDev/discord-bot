using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SubscriptionChangeFlowV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChangeType",
                table: "PlanUpgradeRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "PlanUpgradeRequests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PaymentSubmittedAt",
                table: "PlanUpgradeRequests",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChangeType",
                table: "PlanUpgradeRequests");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "PlanUpgradeRequests");

            migrationBuilder.DropColumn(
                name: "PaymentSubmittedAt",
                table: "PlanUpgradeRequests");
        }
    }
}
