using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReactionRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReactionRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MessageDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RoleDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ButtonCustomId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ButtonLabel = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReactionRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReactionRoles_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReactionRoles_ButtonCustomId",
                table: "ReactionRoles",
                column: "ButtonCustomId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReactionRoles_GuildId_IsActive_CreatedAt",
                table: "ReactionRoles",
                columns: new[] { "GuildId", "IsActive", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReactionRoles");
        }
    }
}
