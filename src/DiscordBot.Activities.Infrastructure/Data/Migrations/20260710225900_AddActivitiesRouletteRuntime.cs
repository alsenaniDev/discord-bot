using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Activities.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActivitiesRouletteRuntime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RouletteGameSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    HostUserDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    HostUsername = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    MinPlayers = table.Column<int>(type: "integer", nullable: false),
                    MaxPlayers = table.Column<int>(type: "integer", nullable: false),
                    WinnerCoins = table.Column<int>(type: "integer", nullable: false),
                    SecondPlaceCoins = table.Column<int>(type: "integer", nullable: false),
                    ParticipationCoins = table.Column<int>(type: "integer", nullable: false),
                    CurrentRound = table.Column<int>(type: "integer", nullable: false),
                    CurrentTurnUserDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    PendingTargetUserDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    PendingActionStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PendingActionExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastSpinResultJson = table.Column<string>(type: "jsonb", nullable: true),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouletteGameSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouletteGameSessions_GameSessions_GameSessionId",
                        column: x => x.GameSessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoulettePlayers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouletteGameSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscordUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Username = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsHost = table.Column<bool>(type: "boolean", nullable: false),
                    IsAlive = table.Column<bool>(type: "boolean", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Eliminations = table.Column<int>(type: "integer", nullable: false),
                    JoinedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EliminatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoulettePlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoulettePlayers_RouletteGameSessions_RouletteGameSessionId",
                        column: x => x.RouletteGameSessionId,
                        principalTable: "RouletteGameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RouletteRounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouletteGameSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoundNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SpinnerUserDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetUserDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    SelectedIndex = table.Column<int>(type: "integer", nullable: true),
                    ResultJson = table.Column<string>(type: "jsonb", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouletteRounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouletteRounds_RouletteGameSessions_RouletteGameSessionId",
                        column: x => x.RouletteGameSessionId,
                        principalTable: "RouletteGameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RouletteBets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouletteRoundId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscordUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BetType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BetValue = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Payout = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    WalletReservationId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    SettledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouletteBets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouletteBets_RouletteRounds_RouletteRoundId",
                        column: x => x.RouletteRoundId,
                        principalTable: "RouletteRounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RouletteBets_RouletteRoundId_DiscordUserId_IdempotencyKey",
                table: "RouletteBets",
                columns: new[] { "RouletteRoundId", "DiscordUserId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouletteGameSessions_GameSessionId",
                table: "RouletteGameSessions",
                column: "GameSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouletteGameSessions_HostUserDiscordId_Status_ExpiresAtUtc",
                table: "RouletteGameSessions",
                columns: new[] { "HostUserDiscordId", "Status", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RouletteGameSessions_Status_ExpiresAtUtc",
                table: "RouletteGameSessions",
                columns: new[] { "Status", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RoulettePlayers_RouletteGameSessionId_DiscordUserId",
                table: "RoulettePlayers",
                columns: new[] { "RouletteGameSessionId", "DiscordUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouletteRounds_RouletteGameSessionId_IdempotencyKey",
                table: "RouletteRounds",
                columns: new[] { "RouletteGameSessionId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouletteRounds_RouletteGameSessionId_RoundNumber",
                table: "RouletteRounds",
                columns: new[] { "RouletteGameSessionId", "RoundNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RouletteBets");

            migrationBuilder.DropTable(
                name: "RoulettePlayers");

            migrationBuilder.DropTable(
                name: "RouletteRounds");

            migrationBuilder.DropTable(
                name: "RouletteGameSessions");
        }
    }
}
