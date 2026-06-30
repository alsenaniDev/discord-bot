using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Guilds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscordGuildId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IconUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    OwnerDiscordUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ResourceSyncRequested = table.Column<bool>(type: "boolean", nullable: false),
                    ResourcesSyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guilds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscordUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Username = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    GlobalName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildChannels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscordChannelId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                    DiscordRoleId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "GuildSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    WelcomeEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    WelcomeChannelId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    WelcomeMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AutoRoleEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AutoRoleId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    LogsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LogChannelId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    TicketsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    TicketCategoryId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildSettings_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetDiscordUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ModeratorDiscordUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogEntries_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketNumber = table.Column<int>(type: "integer", nullable: false),
                    OwnerDiscordUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ChannelDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tickets_Guilds_GuildId",
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

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_DiscordGuildId",
                table: "Guilds",
                column: "DiscordGuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildSettings_GuildId",
                table: "GuildSettings",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_GuildId_CreatedAt",
                table: "LogEntries",
                columns: new[] { "GuildId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ChannelDiscordId",
                table: "Tickets",
                column: "ChannelDiscordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_GuildId_Status",
                table: "Tickets",
                columns: new[] { "GuildId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_GuildId_TicketNumber",
                table: "Tickets",
                columns: new[] { "GuildId", "TicketNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DiscordUserId",
                table: "Users",
                column: "DiscordUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildChannels");

            migrationBuilder.DropTable(
                name: "GuildRoles");

            migrationBuilder.DropTable(
                name: "GuildSettings");

            migrationBuilder.DropTable(
                name: "LogEntries");

            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Guilds");
        }
    }
}
