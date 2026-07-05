using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscordGamesHub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GamePlayers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Username = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TotalPoints = table.Column<int>(type: "integer", nullable: false),
                    GamesPlayed = table.Column<int>(type: "integer", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    Losses = table.Column<int>(type: "integer", nullable: false),
                    CurrentStreak = table.Column<int>(type: "integer", nullable: false),
                    BestStreak = table.Column<int>(type: "integer", nullable: false),
                    LastPlayedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamePlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GamePlayers_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildGamesSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    GamesChannelDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    AutoPostPanel = table.Column<bool>(type: "boolean", nullable: false),
                    GamesPanelMessageDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildGamesSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildGamesSettings_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlatformGameDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IconUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ActivityRoute = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    RequiredPlan = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsEnabledGlobally = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultPointsPerWin = table.Column<int>(type: "integer", nullable: false),
                    DefaultCooldownSeconds = table.Column<int>(type: "integer", nullable: false),
                    DefaultMaxPlaysPerDay = table.Column<int>(type: "integer", nullable: false),
                    SupportsScores = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsLeaderboard = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsResultPublishing = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformGameDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameContent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlatformGameDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DataJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameContent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameContent_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameContent_PlatformGameDefinitions_PlatformGameDefinitionId",
                        column: x => x.PlatformGameDefinitionId,
                        principalTable: "PlatformGameDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlatformGameDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ChannelDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Username = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: true),
                    Won = table.Column<bool>(type: "boolean", nullable: true),
                    PointsAwarded = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameSessions_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GameSessions_PlatformGameDefinitions_PlatformGameDefinition~",
                        column: x => x.PlatformGameDefinitionId,
                        principalTable: "PlatformGameDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GuildGameSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlatformGameDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabledForGuild = table.Column<bool>(type: "boolean", nullable: false),
                    PointsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PointsPerWin = table.Column<int>(type: "integer", nullable: false),
                    CooldownSeconds = table.Column<int>(type: "integer", nullable: false),
                    MaxPlaysPerDay = table.Column<int>(type: "integer", nullable: false),
                    PublishResultAfterGame = table.Column<bool>(type: "boolean", nullable: false),
                    PublishLeaderboardAfterGame = table.Column<bool>(type: "boolean", nullable: false),
                    PublishOnlyWins = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildGameSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildGameSettings_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuildGameSettings_PlatformGameDefinitions_PlatformGameDefin~",
                        column: x => x.PlatformGameDefinitionId,
                        principalTable: "PlatformGameDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GameResultPublishActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    GameSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameResultPublishActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameResultPublishActions_GameSessions_GameSessionId",
                        column: x => x.GameSessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameResultPublishActions_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "PlatformGameDefinitions",
                columns: new[] { "Id", "ActivityRoute", "CreatedAt", "DefaultCooldownSeconds", "DefaultMaxPlaysPerDay", "DefaultPointsPerWin", "Description", "IconUrl", "IsEnabledGlobally", "Key", "Name", "RequiredPlan", "SupportsLeaderboard", "SupportsResultPublishing", "SupportsScores", "UpdatedAt" },
                values: new object[] { new Guid("8f763e4f-d09e-48f5-b77b-406ecef81f98"), "/games/quiz", new DateTimeOffset(new DateTime(2026, 7, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 30, 10, 10, "جاوب على الأسئلة واكسب نقاط.", null, true, "quiz", "تحدي الأسئلة", "free", true, true, true, new DateTimeOffset(new DateTime(2026, 7, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_GameContent_GuildId",
                table: "GameContent",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_GameContent_PlatformGameDefinitionId_GuildId_IsEnabled",
                table: "GameContent",
                columns: new[] { "PlatformGameDefinitionId", "GuildId", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_GamePlayers_GuildId_TotalPoints",
                table: "GamePlayers",
                columns: new[] { "GuildId", "TotalPoints" });

            migrationBuilder.CreateIndex(
                name: "IX_GamePlayers_GuildId_UserDiscordId",
                table: "GamePlayers",
                columns: new[] { "GuildId", "UserDiscordId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameResultPublishActions_GameSessionId",
                table: "GameResultPublishActions",
                column: "GameSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_GameResultPublishActions_GuildId",
                table: "GameResultPublishActions",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_GameResultPublishActions_Status",
                table: "GameResultPublishActions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_GuildId_UserDiscordId_StartedAt",
                table: "GameSessions",
                columns: new[] { "GuildId", "UserDiscordId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_PlatformGameDefinitionId",
                table: "GameSessions",
                column: "PlatformGameDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_Status",
                table: "GameSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_GuildGameSettings_GuildId_PlatformGameDefinitionId",
                table: "GuildGameSettings",
                columns: new[] { "GuildId", "PlatformGameDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildGameSettings_PlatformGameDefinitionId",
                table: "GuildGameSettings",
                column: "PlatformGameDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildGamesSettings_GuildId",
                table: "GuildGamesSettings",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformGameDefinitions_Key",
                table: "PlatformGameDefinitions",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameContent");

            migrationBuilder.DropTable(
                name: "GamePlayers");

            migrationBuilder.DropTable(
                name: "GameResultPublishActions");

            migrationBuilder.DropTable(
                name: "GuildGameSettings");

            migrationBuilder.DropTable(
                name: "GuildGamesSettings");

            migrationBuilder.DropTable(
                name: "GameSessions");

            migrationBuilder.DropTable(
                name: "PlatformGameDefinitions");
        }
    }
}
