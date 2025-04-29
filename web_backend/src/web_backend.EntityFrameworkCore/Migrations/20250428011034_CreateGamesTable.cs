using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_backend.Migrations
{
    /// <inheritdoc />
    public partial class CreateGamesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Game",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GameUrl = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    HomeTeam = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AwayTeam = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    HomeScore = table.Column<int>(type: "int", nullable: false),
                    AwayScore = table.Column<int>(type: "int", nullable: false),
                    Broadcasters = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Game", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Game");
        }
    }
}
