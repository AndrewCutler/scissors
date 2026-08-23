using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scissors.API.Migrations
{
    /// <inheritdoc />
    public partial class ClippingDeletedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Clippings",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Clippings");
        }
    }
}
