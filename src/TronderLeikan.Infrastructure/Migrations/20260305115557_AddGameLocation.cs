using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TronderLeikan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGameLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Games",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "Games");
        }
    }
}
