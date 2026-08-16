using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scissors.API.Migrations
{
    /// <inheritdoc />
    public partial class ExternalIdentitiesFKFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExternalIdentities_Users_UserId1",
                table: "ExternalIdentities");

            migrationBuilder.DropIndex(
                name: "IX_ExternalIdentities_UserId1",
                table: "ExternalIdentities");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "ExternalIdentities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "ExternalIdentities",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIdentities_UserId1",
                table: "ExternalIdentities",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ExternalIdentities_Users_UserId1",
                table: "ExternalIdentities",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
