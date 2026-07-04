using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiPanelSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildPanels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ChannelDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MessageDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    RefreshRequested = table.Column<bool>(type: "boolean", nullable: false),
                    LastPublishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildPanels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildPanels_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildPanelButtons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PanelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Emoji = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Style = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ActionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TicketTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ResponseMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RoleDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildPanelButtons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildPanelButtons_GuildPanels_PanelId",
                        column: x => x.PanelId,
                        principalTable: "GuildPanels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuildPanelButtons_PanelId",
                table: "GuildPanelButtons",
                column: "PanelId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildPanels_ChannelDiscordId",
                table: "GuildPanels",
                column: "ChannelDiscordId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildPanels_GuildId",
                table: "GuildPanels",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildPanels_MessageDiscordId",
                table: "GuildPanels",
                column: "MessageDiscordId");

            // Backfill the legacy one-panel-per-guild settings. Keep the old columns until a later cleanup migration.
            migrationBuilder.Sql("""
                INSERT INTO "GuildPanels" (
                    "Id", "GuildId", "Name", "Title", "Description", "ImageUrl",
                    "ChannelDiscordId", "MessageDiscordId", "IsEnabled", "IsPublished",
                    "RefreshRequested", "LastPublishedAtUtc", "CreatedAt", "UpdatedAt")
                SELECT
                    md5(gs."GuildId"::text || ':default-support-panel')::uuid,
                    gs."GuildId", 'Default Support Panel',
                    COALESCE(NULLIF(gs."CommandPanelTitle", ''), 'How can we help?'),
                    COALESCE(gs."CommandPanelDescription", ''), gs."CommandPanelImageUrl",
                    COALESCE(gs."CommandPanelChannelId", ''), gs."CommandPanelMessageId",
                    gs."CommandPanelEnabled", COALESCE(gs."CommandPanelMessageId", '') <> '',
                    gs."CommandPanelRefreshRequested" OR (gs."CommandPanelEnabled" AND COALESCE(gs."CommandPanelMessageId", '') = ''),
                    NULL, NOW(), NOW()
                FROM "GuildSettings" gs
                WHERE gs."CommandPanelEnabled"
                   OR COALESCE(gs."CommandPanelTitle", '') <> ''
                   OR COALESCE(gs."CommandPanelButtonsJson", '') NOT IN ('', '[]')
                ON CONFLICT ("Id") DO NOTHING;

                INSERT INTO "GuildPanelButtons" (
                    "Id", "PanelId", "Label", "Emoji", "Style", "ActionType",
                    "TicketTypeId", "Url", "ResponseMessage", "RoleDiscordId",
                    "SortOrder", "IsEnabled", "CreatedAt", "UpdatedAt")
                SELECT
                    md5(gs."GuildId"::text || ':default-button:' || button.ordinality::text)::uuid,
                    md5(gs."GuildId"::text || ':default-support-panel')::uuid,
                    COALESCE(NULLIF(button.value->>'label', ''), NULLIF(button.value->>'Label', ''), 'Create ticket'),
                    NULL,
                    CASE COALESCE(button.value->>'style', button.value->>'Style', 'Secondary')
                        WHEN 'Primary' THEN 'Primary' WHEN 'Success' THEN 'Success'
                        WHEN 'Danger' THEN 'Danger' ELSE 'Secondary' END,
                    'CreateTicket', NULL, NULL, NULL, NULL,
                    COALESCE((COALESCE(button.value->>'order', button.value->>'Order'))::integer, button.ordinality::integer - 1),
                    COALESCE((COALESCE(button.value->>'enabled', button.value->>'Enabled'))::boolean, true),
                    NOW(), NOW()
                FROM "GuildSettings" gs
                CROSS JOIN LATERAL jsonb_array_elements(
                    CASE WHEN COALESCE(gs."CommandPanelButtonsJson", '') = '' THEN '[]'::jsonb
                         ELSE gs."CommandPanelButtonsJson"::jsonb END
                ) WITH ORDINALITY AS button(value, ordinality)
                WHERE EXISTS (SELECT 1 FROM "GuildPanels" p WHERE p."Id" = md5(gs."GuildId"::text || ':default-support-panel')::uuid)
                ON CONFLICT ("Id") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildPanelButtons");

            migrationBuilder.DropTable(
                name: "GuildPanels");
        }
    }
}
