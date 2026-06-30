using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameDiscordResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildChannels");

            migrationBuilder.DropTable(
                name: "GuildRoles");

            migrationBuilder.CreateTable(
                name: "DiscordChannels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscordChannelId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscordChannels_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiscordRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscordRoleId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Color = table.Column<int>(type: "integer", nullable: true),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    IsManaged = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscordRoles_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscordChannels_GuildId_DiscordChannelId",
                table: "DiscordChannels",
                columns: new[] { "GuildId", "DiscordChannelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscordChannels_GuildId_Type",
                table: "DiscordChannels",
                columns: new[] { "GuildId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_DiscordRoles_GuildId_DiscordRoleId",
                table: "DiscordRoles",
                columns: new[] { "GuildId", "DiscordRoleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscordChannels");

            migrationBuilder.DropTable(
                name: "DiscordRoles");

            migrationBuilder.CreateTable(
                name: "GuildChannels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DiscordChannelId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildChannels_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DiscordRoleId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildRoles_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuildChannels_GuildId_DiscordChannelId",
                table: "GuildChannels",
                columns: new[] { "GuildId", "DiscordChannelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildChannels_GuildId_Type",
                table: "GuildChannels",
                columns: new[] { "GuildId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_GuildRoles_GuildId_DiscordRoleId",
                table: "GuildRoles",
                columns: new[] { "GuildId", "DiscordRoleId" },
                unique: true);
        }
    }
}
