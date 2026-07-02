using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UnifyGuildPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "GuildPermissionRoles" g
                SET "Permissions" = g."Permissions" | sub."MergedPermissions",
                    "UpdatedAt" = NOW()
                FROM (
                    SELECT m."GuildId",
                           m."RoleDiscordId",
                           (CASE WHEN m."CanWarn" THEN 1 ELSE 0 END) |
                           (CASE WHEN m."CanKick" THEN 2 ELSE 0 END) |
                           (CASE WHEN m."CanClearMessages" THEN 8 ELSE 0 END) |
                           (CASE WHEN m."CanViewLogs" THEN 32 ELSE 0 END) |
                           (CASE WHEN m."CanViewWarnings" THEN 262144 ELSE 0 END) |
                           (CASE WHEN m."CanViewModerationCases" THEN 524288 ELSE 0 END) AS "MergedPermissions"
                    FROM "ModerationPermissionRoles" m
                ) sub
                WHERE g."GuildId" = sub."GuildId"
                  AND g."DiscordRoleId" = sub."RoleDiscordId";

                INSERT INTO "GuildPermissionRoles" ("Id", "GuildId", "Name", "DiscordRoleId", "Permissions", "CreatedAt", "UpdatedAt")
                SELECT gen_random_uuid(),
                       m."GuildId",
                       COALESCE(dr."Name", 'Moderation Role'),
                       m."RoleDiscordId",
                       (CASE WHEN m."CanWarn" THEN 1 ELSE 0 END) |
                       (CASE WHEN m."CanKick" THEN 2 ELSE 0 END) |
                       (CASE WHEN m."CanClearMessages" THEN 8 ELSE 0 END) |
                       (CASE WHEN m."CanViewLogs" THEN 32 ELSE 0 END) |
                       (CASE WHEN m."CanViewWarnings" THEN 262144 ELSE 0 END) |
                       (CASE WHEN m."CanViewModerationCases" THEN 524288 ELSE 0 END),
                       NOW(),
                       NOW()
                FROM "ModerationPermissionRoles" m
                LEFT JOIN "DiscordRoles" dr
                    ON dr."GuildId" = m."GuildId" AND dr."DiscordRoleId" = m."RoleDiscordId"
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "GuildPermissionRoles" g
                    WHERE g."GuildId" = m."GuildId"
                      AND g."DiscordRoleId" = m."RoleDiscordId"
                );
                """);

            migrationBuilder.DropTable(
                name: "GuildStaff");

            migrationBuilder.DropTable(
                name: "ModerationPermissionRoles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildStaff",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByDiscordUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DiscordUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildStaff", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildStaff_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModerationPermissionRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    CanClearMessages = table.Column<bool>(type: "boolean", nullable: false),
                    CanKick = table.Column<bool>(type: "boolean", nullable: false),
                    CanViewLogs = table.Column<bool>(type: "boolean", nullable: false),
                    CanViewModerationCases = table.Column<bool>(type: "boolean", nullable: false),
                    CanViewWarnings = table.Column<bool>(type: "boolean", nullable: false),
                    CanWarn = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RoleDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModerationPermissionRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModerationPermissionRoles_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuildStaff_GuildId_DiscordUserId",
                table: "GuildStaff",
                columns: new[] { "GuildId", "DiscordUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModerationPermissionRoles_GuildId_RoleDiscordId",
                table: "ModerationPermissionRoles",
                columns: new[] { "GuildId", "RoleDiscordId" },
                unique: true);
        }
    }
}
