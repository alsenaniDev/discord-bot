using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Activities.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityInstanceRouletteIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ActivitySessions_DiscordGuildId_DiscordChannelId_DiscordAct~",
                table: "ActivitySessions",
                columns: new[] { "DiscordGuildId", "DiscordChannelId", "DiscordActivityInstanceId", "GameKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ActivitySessions_DiscordGuildId_DiscordChannelId_DiscordAct~",
                table: "ActivitySessions");
        }
    }
}
