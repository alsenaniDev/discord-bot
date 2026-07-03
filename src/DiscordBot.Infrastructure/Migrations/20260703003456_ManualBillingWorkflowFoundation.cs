using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ManualBillingWorkflowFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminOverrideReason",
                table: "PlanUpgradeRequests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledAt",
                table: "PlanUpgradeRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CancelledByUserId",
                table: "PlanUpgradeRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedTotalAmount",
                table: "PlanUpgradeRequests",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RequestExpiresAt",
                table: "PlanUpgradeRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RequestedPlanMonthlyPrice",
                table: "PlanUpgradeRequests",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_PlanUpgradeRequests_CancelledByUserId",
                table: "PlanUpgradeRequests",
                column: "CancelledByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlanUpgradeRequests_Users_CancelledByUserId",
                table: "PlanUpgradeRequests",
                column: "CancelledByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql("""
                UPDATE "PlanUpgradeRequests" AS r
                SET
                    "RequestedPlanMonthlyPrice" = p."MonthlyPrice",
                    "EstimatedTotalAmount" = p."MonthlyPrice" * r."DurationMonths"
                FROM "SubscriptionPlans" AS p
                WHERE p."Id" = r."RequestedPlanId";
                """);

            migrationBuilder.Sql("""
                UPDATE "PlanUpgradeRequests"
                SET "Status" = CASE "Status"
                    WHEN 0 THEN 1
                    WHEN 1 THEN 5
                    WHEN 2 THEN 6
                    ELSE "Status"
                END;
                """);

            migrationBuilder.Sql("""
                UPDATE "PlanUpgradeRequests"
                SET "RequestExpiresAt" = "CreatedAt" + INTERVAL '14 days'
                WHERE "Status" = 1 AND "RequestExpiresAt" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlanUpgradeRequests_Users_CancelledByUserId",
                table: "PlanUpgradeRequests");

            migrationBuilder.DropIndex(
                name: "IX_PlanUpgradeRequests_CancelledByUserId",
                table: "PlanUpgradeRequests");

            migrationBuilder.DropColumn(
                name: "AdminOverrideReason",
                table: "PlanUpgradeRequests");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "PlanUpgradeRequests");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "PlanUpgradeRequests");

            migrationBuilder.DropColumn(
                name: "EstimatedTotalAmount",
                table: "PlanUpgradeRequests");

            migrationBuilder.DropColumn(
                name: "RequestExpiresAt",
                table: "PlanUpgradeRequests");

            migrationBuilder.DropColumn(
                name: "RequestedPlanMonthlyPrice",
                table: "PlanUpgradeRequests");
        }
    }
}
