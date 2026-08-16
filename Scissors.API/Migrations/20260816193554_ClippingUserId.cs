using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scissors.API.Migrations
{
    /// <inheritdoc />
    public partial class ClippingUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Clippings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Clippings");
        }
    }
}
