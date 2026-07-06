using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRouletteMultiplayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameWallets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Balance = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameWallets", x => x.Id);
                    table.CheckConstraint("CK_GameWallets_Balance_NonNegative", "\"Balance\" >= 0");
                    table.ForeignKey(
                        name: "FK_GameWallets_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameWalletTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameWalletTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameWalletTransactions_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RouletteGuildSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    MinPlayers = table.Column<int>(type: "integer", nullable: false),
                    MaxPlayers = table.Column<int>(type: "integer", nullable: false),
                    WinnerCoins = table.Column<int>(type: "integer", nullable: false),
                    SecondPlaceCoins = table.Column<int>(type: "integer", nullable: false),
                    ParticipationCoins = table.Column<int>(type: "integer", nullable: false),
                    JoinWindowSeconds = table.Column<int>(type: "integer", nullable: false),
                    TurnSeconds = table.Column<int>(type: "integer", nullable: false),
                    AnnounceRoomCreated = table.Column<bool>(type: "boolean", nullable: false),
                    AnnounceWinner = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouletteGuildSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouletteGuildSettings_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RouletteRooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlatformGameDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    HostUserDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    HostUsername = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    MinPlayers = table.Column<int>(type: "integer", nullable: false),
                    MaxPlayers = table.Column<int>(type: "integer", nullable: false),
                    WinnerCoins = table.Column<int>(type: "integer", nullable: false),
                    SecondPlaceCoins = table.Column<int>(type: "integer", nullable: false),
                    ParticipationCoins = table.Column<int>(type: "integer", nullable: false),
                    CurrentRound = table.Column<int>(type: "integer", nullable: false),
                    InviteMessageDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouletteRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouletteRooms_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RouletteRooms_PlatformGameDefinitions_PlatformGameDefinitio~",
                        column: x => x.PlatformGameDefinitionId,
                        principalTable: "PlatformGameDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RouletteJoinIntents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    RouletteRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ChannelDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouletteJoinIntents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouletteJoinIntents_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RouletteJoinIntents_RouletteRooms_RouletteRoomId",
                        column: x => x.RouletteRoomId,
                        principalTable: "RouletteRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoulettePublishActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    RouletteRoomId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_RoulettePublishActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoulettePublishActions_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoulettePublishActions_RouletteRooms_RouletteRoomId",
                        column: x => x.RouletteRoomId,
                        principalTable: "RouletteRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RouletteRoomPlayers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouletteRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Username = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsHost = table.Column<bool>(type: "boolean", nullable: false),
                    IsAlive = table.Column<bool>(type: "boolean", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Eliminations = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EliminatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouletteRoomPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouletteRoomPlayers_RouletteRooms_RouletteRoomId",
                        column: x => x.RouletteRoomId,
                        principalTable: "RouletteRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RouletteRoundActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouletteRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoundNumber = table.Column<int>(type: "integer", nullable: false),
                    ActorUserDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetUserDiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ActionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DataJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouletteRoundActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouletteRoundActions_RouletteRooms_RouletteRoomId",
                        column: x => x.RouletteRoomId,
                        principalTable: "RouletteRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "PlatformGameDefinitions",
                columns: new[] { "Id", "ActivityRoute", "CreatedAt", "DefaultCooldownSeconds", "DefaultMaxPlaysPerDay", "DefaultPointsPerWin", "Description", "IconUrl", "IsEnabledGlobally", "Key", "Name", "PlayMode", "RequiredPlan", "SupportsLeaderboard", "SupportsResultPublishing", "SupportsScores", "UpdatedAt" },
                values: new object[] { new Guid("77cfca31-9574-4f30-8ac5-e87d1eb65663"), "/games/roulette", new DateTimeOffset(new DateTime(2026, 7, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 30, 10, 0, "لعبة جماعية تعتمد على الحظ والتحدي بين الأعضاء.", null, true, "roulette", "الروليت", "Multiplayer", "pro", true, true, true, new DateTimeOffset(new DateTime(2026, 7, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_GameWallets_GuildId_UserDiscordId",
                table: "GameWallets",
                columns: new[] { "GuildId", "UserDiscordId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameWalletTransactions_GuildId_UserDiscordId_CreatedAt",
                table: "GameWalletTransactions",
                columns: new[] { "GuildId", "UserDiscordId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GameWalletTransactions_ReferenceId",
                table: "GameWalletTransactions",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_GameWalletTransactions_ReferenceId_UserDiscordId_Type",
                table: "GameWalletTransactions",
                columns: new[] { "ReferenceId", "UserDiscordId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouletteGuildSettings_GuildId",
                table: "RouletteGuildSettings",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouletteJoinIntents_GuildId",
                table: "RouletteJoinIntents",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_RouletteJoinIntents_RouletteRoomId",
                table: "RouletteJoinIntents",
                column: "RouletteRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_RouletteJoinIntents_UserDiscordId_Status_ExpiresAt",
                table: "RouletteJoinIntents",
                columns: new[] { "UserDiscordId", "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RoulettePublishActions_GuildId",
                table: "RoulettePublishActions",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_RoulettePublishActions_RouletteRoomId",
                table: "RoulettePublishActions",
                column: "RouletteRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_RoulettePublishActions_Status_CreatedAt",
                table: "RoulettePublishActions",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RouletteRoomPlayers_RouletteRoomId_UserDiscordId",
                table: "RouletteRoomPlayers",
                columns: new[] { "RouletteRoomId", "UserDiscordId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouletteRooms_GuildId_ChannelDiscordId_Status",
                table: "RouletteRooms",
                columns: new[] { "GuildId", "ChannelDiscordId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RouletteRooms_PlatformGameDefinitionId",
                table: "RouletteRooms",
                column: "PlatformGameDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_RouletteRoundActions_RouletteRoomId_RoundNumber",
                table: "RouletteRoundActions",
                columns: new[] { "RouletteRoomId", "RoundNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameWallets");

            migrationBuilder.DropTable(
                name: "GameWalletTransactions");

            migrationBuilder.DropTable(
                name: "RouletteGuildSettings");

            migrationBuilder.DropTable(
                name: "RouletteJoinIntents");

            migrationBuilder.DropTable(
                name: "RoulettePublishActions");

            migrationBuilder.DropTable(
                name: "RouletteRoomPlayers");

            migrationBuilder.DropTable(
                name: "RouletteRoundActions");

            migrationBuilder.DropTable(
                name: "RouletteRooms");

            migrationBuilder.DeleteData(
                table: "PlatformGameDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("77cfca31-9574-4f30-8ac5-e87d1eb65663"));
        }
    }
}
