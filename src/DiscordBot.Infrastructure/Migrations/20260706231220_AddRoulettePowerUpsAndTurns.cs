using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DiscordBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoulettePowerUpsAndTurns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentTurnUserDiscordId",
                table: "RouletteRooms",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSpinResultJson",
                table: "RouletteRooms",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PendingActionExpiresAt",
                table: "RouletteRooms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingActionStatus",
                table: "RouletteRooms",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingTargetUserDiscordId",
                table: "RouletteRooms",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GamePowerUpDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Icon = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsEnabledGlobally = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultPrice = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamePowerUpDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildPowerUpSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    GamePowerUpDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabledForGuild = table.Column<bool>(type: "boolean", nullable: false),
                    Price = table.Column<int>(type: "integer", nullable: false),
                    MaxUsesPerGame = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildPowerUpSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildPowerUpSettings_GamePowerUpDefinitions_GamePowerUpDefi~",
                        column: x => x.GamePowerUpDefinitionId,
                        principalTable: "GamePowerUpDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GuildPowerUpSettings_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerPowerUpInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    GamePowerUpDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerPowerUpInventories", x => x.Id);
                    table.CheckConstraint("CK_PlayerPowerUpInventories_Quantity_NonNegative", "\"Quantity\" >= 0");
                    table.ForeignKey(
                        name: "FK_PlayerPowerUpInventories_GamePowerUpDefinitions_GamePowerUp~",
                        column: x => x.GamePowerUpDefinitionId,
                        principalTable: "GamePowerUpDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerPowerUpInventories_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoulettePowerUpUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouletteRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    GamePowerUpDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoundNumber = table.Column<int>(type: "integer", nullable: false),
                    ResultJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoulettePowerUpUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoulettePowerUpUsages_GamePowerUpDefinitions_GamePowerUpDef~",
                        column: x => x.GamePowerUpDefinitionId,
                        principalTable: "GamePowerUpDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoulettePowerUpUsages_RouletteRooms_RouletteRoomId",
                        column: x => x.RouletteRoomId,
                        principalTable: "RouletteRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "GamePowerUpDefinitions",
                columns: new[] { "Id", "CreatedAt", "DefaultPrice", "Description", "Icon", "IsEnabledGlobally", "Key", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("15125967-74cc-4809-9397-2c5d30f38bd8"), new DateTimeOffset(new DateTime(2026, 7, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 100, "يحميك من الإقصاء مرة واحدة.", "🛡️", true, "shield", "الدرع", new DateTimeOffset(new DateTime(2026, 7, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("5bf04af4-0d20-490c-aa9f-a82cc6cb02b7"), new DateTimeOffset(new DateTime(2026, 7, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 150, "يعكس الإقصاء على اللاعب الذي لف العجلة.", "🔁", true, "reverse", "عكس الهجمة", new DateTimeOffset(new DateTime(2026, 7, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("676c428c-fc17-44ee-8bef-a2ad8ed4ad88"), new DateTimeOffset(new DateTime(2026, 7, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 120, "يعيد تدوير العجلة مرة واحدة.", "🎡", true, "respin", "إعادة اللف", new DateTimeOffset(new DateTime(2026, 7, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_GamePowerUpDefinitions_Key",
                table: "GamePowerUpDefinitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildPowerUpSettings_GamePowerUpDefinitionId",
                table: "GuildPowerUpSettings",
                column: "GamePowerUpDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildPowerUpSettings_GuildId_GamePowerUpDefinitionId",
                table: "GuildPowerUpSettings",
                columns: new[] { "GuildId", "GamePowerUpDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerPowerUpInventories_GamePowerUpDefinitionId",
                table: "PlayerPowerUpInventories",
                column: "GamePowerUpDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerPowerUpInventories_GuildId_UserDiscordId_GamePowerUpD~",
                table: "PlayerPowerUpInventories",
                columns: new[] { "GuildId", "UserDiscordId", "GamePowerUpDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoulettePowerUpUsages_GamePowerUpDefinitionId",
                table: "RoulettePowerUpUsages",
                column: "GamePowerUpDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoulettePowerUpUsages_RouletteRoomId_UserDiscordId_GamePowe~",
                table: "RoulettePowerUpUsages",
                columns: new[] { "RouletteRoomId", "UserDiscordId", "GamePowerUpDefinitionId", "RoundNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildPowerUpSettings");

            migrationBuilder.DropTable(
                name: "PlayerPowerUpInventories");

            migrationBuilder.DropTable(
                name: "RoulettePowerUpUsages");

            migrationBuilder.DropTable(
                name: "GamePowerUpDefinitions");

            migrationBuilder.DropColumn(
                name: "CurrentTurnUserDiscordId",
                table: "RouletteRooms");

            migrationBuilder.DropColumn(
                name: "LastSpinResultJson",
                table: "RouletteRooms");

            migrationBuilder.DropColumn(
                name: "PendingActionExpiresAt",
                table: "RouletteRooms");

            migrationBuilder.DropColumn(
                name: "PendingActionStatus",
                table: "RouletteRooms");

            migrationBuilder.DropColumn(
                name: "PendingTargetUserDiscordId",
                table: "RouletteRooms");
        }
    }
}
