using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketTimelineEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DeliveryFailed",
                table: "TicketOutboundMessages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryFailureReason",
                table: "TicketOutboundMessages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StaffReplyQueuedTimelineEventId",
                table: "TicketOutboundMessages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "TicketTimelineEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActorDiscordUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ActorDisplayName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DiscordMessageId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    RelatedTimelineEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    MetadataJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketTimelineEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketTimelineEvents_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketTimelineEvents_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketOutboundMessages_GuildId_IsDelivered_DeliveryFailed_C~",
                table: "TicketOutboundMessages",
                columns: new[] { "GuildId", "IsDelivered", "DeliveryFailed", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketTimelineEvents_GuildId_OccurredAt",
                table: "TicketTimelineEvents",
                columns: new[] { "GuildId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketTimelineEvents_TicketId_DiscordMessageId",
                table: "TicketTimelineEvents",
                columns: new[] { "TicketId", "DiscordMessageId" },
                unique: true,
                filter: "\"DiscordMessageId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTimelineEvents_TicketId_OccurredAt",
                table: "TicketTimelineEvents",
                columns: new[] { "TicketId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketTimelineEvents");

            migrationBuilder.DropIndex(
                name: "IX_TicketOutboundMessages_GuildId_IsDelivered_DeliveryFailed_C~",
                table: "TicketOutboundMessages");

            migrationBuilder.DropColumn(
                name: "DeliveryFailed",
                table: "TicketOutboundMessages");

            migrationBuilder.DropColumn(
                name: "DeliveryFailureReason",
                table: "TicketOutboundMessages");

            migrationBuilder.DropColumn(
                name: "StaffReplyQueuedTimelineEventId",
                table: "TicketOutboundMessages");
        }
    }
}
