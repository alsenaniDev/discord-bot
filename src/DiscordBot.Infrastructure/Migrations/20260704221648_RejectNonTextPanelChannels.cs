using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RejectNonTextPanelChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Earlier panel editors exposed voice/category resources. Those Discord message IDs may point to
            // voice-channel chat and must not count as a successfully published text-channel panel.
            migrationBuilder.Sql("""
                UPDATE "GuildPanels" AS panel
                SET "IsPublished" = FALSE,
                    "RefreshRequested" = FALSE,
                    "MessageDiscordId" = NULL,
                    "LastPublishedAtUtc" = NULL,
                    "LastPublishFailed" = TRUE,
                    "LastPublishFailureReason" = 'The configured destination is not a Discord text channel. Select a text channel and publish again.',
                    "LastPublishAttemptedAtUtc" = NOW(),
                    "UpdatedAt" = NOW()
                WHERE EXISTS (
                    SELECT 1
                    FROM "DiscordChannels" AS channel
                    WHERE channel."GuildId" = panel."GuildId"
                      AND channel."DiscordChannelId" = panel."ChannelDiscordId"
                      AND channel."Type" <> 'Text'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data repair is intentionally not reversed; the previous Discord message target was invalid.
        }
    }
}
