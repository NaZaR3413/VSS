using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_backend.Migrations
{
    /// <inheritdoc />
    public partial class freeLivesteramTableUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FreeLivestream",
                table: "Livestream",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FreeLivestream",
                table: "Livestream");
        }
    }
}
