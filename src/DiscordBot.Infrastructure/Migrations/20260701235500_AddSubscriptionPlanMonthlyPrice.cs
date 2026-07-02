using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlanMonthlyPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyPrice",
                table: "SubscriptionPlans",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                UPDATE "SubscriptionPlans" SET "MonthlyPrice" = 0.00 WHERE "Key" = 'free';
                UPDATE "SubscriptionPlans" SET "MonthlyPrice" = 9.99 WHERE "Key" = 'basic';
                UPDATE "SubscriptionPlans" SET "MonthlyPrice" = 19.99 WHERE "Key" = 'pro';
                UPDATE "SubscriptionPlans" SET "MonthlyPrice" = 29.99 WHERE "Key" = 'premium';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthlyPrice",
                table: "SubscriptionPlans");
        }
    }
}
