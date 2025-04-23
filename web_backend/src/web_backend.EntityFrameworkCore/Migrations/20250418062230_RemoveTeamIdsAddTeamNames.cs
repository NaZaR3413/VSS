using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTeamIdsAddTeamNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwayTeamId",
                table: "Livestream");

            migrationBuilder.DropColumn(
                name: "HomeTeamId",
                table: "Livestream");

            migrationBuilder.AddColumn<string>(
                name: "AwayTeam",
                table: "Livestream",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HomeTeam",
                table: "Livestream",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwayTeam",
                table: "Livestream");

            migrationBuilder.DropColumn(
                name: "HomeTeam",
                table: "Livestream");

            migrationBuilder.AddColumn<Guid>(
                name: "AwayTeamId",
                table: "Livestream",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "HomeTeamId",
                table: "Livestream",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
