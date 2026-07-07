using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DiscordBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGamePluginArchitecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    FrontendUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    BackendUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ActivityRoute = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ManifestJson = table.Column<string>(type: "jsonb", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameVersions_PlatformGameDefinitions_GameDefinitionId",
                        column: x => x.GameDefinitionId,
                        principalTable: "PlatformGameDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GameVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ChannelDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    UserDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    EventType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameEvents_GameVersions_GameVersionId",
                        column: x => x.GameVersionId,
                        principalTable: "GameVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GameEvents_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GameRuntimeTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    GameKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GameVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ChannelDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UserDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Mode = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameRuntimeTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameRuntimeTokens_GameVersions_GameVersionId",
                        column: x => x.GameVersionId,
                        principalTable: "GameVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameRuntimeTokens_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameSandboxAccess",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UserDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSandboxAccess", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameSandboxAccess_GameVersions_GameVersionId",
                        column: x => x.GameVersionId,
                        principalTable: "GameVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameBotPublishActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    MessageJson = table.Column<string>(type: "jsonb", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameBotPublishActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameBotPublishActions_GameEvents_GameEventId",
                        column: x => x.GameEventId,
                        principalTable: "GameEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameBotPublishActions_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "GameVersions",
                columns: new[] { "Id", "ActivityRoute", "BackendUrl", "CreatedAt", "FrontendUrl", "GameDefinitionId", "ManifestJson", "Notes", "PublishedAt", "Status", "UpdatedAt", "Version" },
                values: new object[,]
                {
                    { new Guid("5d9982f9-6da7-41d8-8074-b241102c84a4"), "/games/quiz", null, new DateTimeOffset(new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("8f763e4f-d09e-48f5-b77b-406ecef81f98"), "{\"key\":\"quiz\",\"name\":\"تحدي الأسئلة\",\"description\":\"جاوب على الأسئلة واكسب نقاط.\",\"playMode\":\"Solo\",\"engineType\":\"Platform\",\"frontendMode\":\"InternalRoute\",\"activityRoute\":\"/games/quiz\",\"requiredPlan\":\"free\",\"supportsWallet\":false,\"supportsLeaderboard\":true,\"supportsPowerUps\":false,\"supportsBotPublishing\":true,\"events\":[\"quiz.completed\"],\"permissions\":[],\"sandboxAllowedOrigins\":[],\"configSchema\":{}}", null, new DateTimeOffset(new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Published", new DateTimeOffset(new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "1.0.0" },
                    { new Guid("ddfdc3c0-53fb-45cb-b5aa-3e942ed9d892"), "/games/roulette", null, new DateTimeOffset(new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("77cfca31-9574-4f30-8ac5-e87d1eb65663"), "{\"key\":\"roulette\",\"name\":\"الروليت\",\"description\":\"لعبة جماعية تعتمد على الحظ والتحدي بين الأعضاء.\",\"playMode\":\"Multiplayer\",\"engineType\":\"Hybrid\",\"frontendMode\":\"InternalRoute\",\"activityRoute\":\"/games/roulette\",\"requiredPlan\":\"pro\",\"supportsWallet\":true,\"supportsLeaderboard\":true,\"supportsPowerUps\":true,\"supportsBotPublishing\":true,\"events\":[\"roulette.room.created\",\"roulette.room.completed\",\"roulette.player.won\"],\"permissions\":[\"wallet.read\",\"wallet.transaction.request\",\"bot.publish.request\"],\"sandboxAllowedOrigins\":[],\"configSchema\":{}}", null, new DateTimeOffset(new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Published", new DateTimeOffset(new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "1.0.0" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameBotPublishActions_GameEventId",
                table: "GameBotPublishActions",
                column: "GameEventId");

            migrationBuilder.CreateIndex(
                name: "IX_GameBotPublishActions_GuildId",
                table: "GameBotPublishActions",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_GameBotPublishActions_Status_CreatedAt",
                table: "GameBotPublishActions",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GameEvents_GameKey_IdempotencyKey",
                table: "GameEvents",
                columns: new[] { "GameKey", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameEvents_GameVersionId",
                table: "GameEvents",
                column: "GameVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_GameEvents_GuildId_EventType_CreatedAt",
                table: "GameEvents",
                columns: new[] { "GuildId", "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GameEvents_Status_CreatedAt",
                table: "GameEvents",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GameRuntimeTokens_ExpiresAt",
                table: "GameRuntimeTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_GameRuntimeTokens_GameVersionId",
                table: "GameRuntimeTokens",
                column: "GameVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_GameRuntimeTokens_GuildId",
                table: "GameRuntimeTokens",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_GameRuntimeTokens_TokenHash",
                table: "GameRuntimeTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameSandboxAccess_GameVersionId_GuildDiscordId_UserDiscordId",
                table: "GameSandboxAccess",
                columns: new[] { "GameVersionId", "GuildDiscordId", "UserDiscordId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameVersions_GameDefinitionId_Status",
                table: "GameVersions",
                columns: new[] { "GameDefinitionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_GameVersions_GameDefinitionId_Version",
                table: "GameVersions",
                columns: new[] { "GameDefinitionId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameBotPublishActions");

            migrationBuilder.DropTable(
                name: "GameRuntimeTokens");

            migrationBuilder.DropTable(
                name: "GameSandboxAccess");

            migrationBuilder.DropTable(
                name: "GameEvents");

            migrationBuilder.DropTable(
                name: "GameVersions");
        }
    }
}
