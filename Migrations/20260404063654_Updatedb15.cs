using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE_API.Migrations
{
    /// <inheritdoc />
    public partial class Updatedb15 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ReportedByUserId",
                table: "BusDamageReports",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusDamageReports_ReportedByUserId",
                table: "BusDamageReports",
                column: "ReportedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BusDamageReports_Users_ReportedByUserId",
                table: "BusDamageReports",
                column: "ReportedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusDamageReports_Users_ReportedByUserId",
                table: "BusDamageReports");

            migrationBuilder.DropIndex(
                name: "IX_BusDamageReports_ReportedByUserId",
                table: "BusDamageReports");

            migrationBuilder.DropColumn(
                name: "ReportedByUserId",
                table: "BusDamageReports");
        }
    }
}
