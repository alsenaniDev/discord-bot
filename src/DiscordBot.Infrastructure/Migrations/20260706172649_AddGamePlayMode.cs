using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGamePlayMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlayMode",
                table: "PlatformGameDefinitions",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "Solo");

            migrationBuilder.UpdateData(
                table: "PlatformGameDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("8f763e4f-d09e-48f5-b77b-406ecef81f98"),
                column: "PlayMode",
                value: "Solo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlayMode",
                table: "PlatformGameDefinitions");
        }
    }
}
