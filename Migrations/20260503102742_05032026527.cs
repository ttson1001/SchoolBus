using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE_API.Migrations
{
    /// <inheritdoc />
    public partial class _05032026527 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CampusId",
                table: "BusStations",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusStations_CampusId",
                table: "BusStations",
                column: "CampusId");

            migrationBuilder.AddForeignKey(
                name: "FK_BusStations_Campuses_CampusId",
                table: "BusStations",
                column: "CampusId",
                principalTable: "Campuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusStations_Campuses_CampusId",
                table: "BusStations");

            migrationBuilder.DropIndex(
                name: "IX_BusStations_CampusId",
                table: "BusStations");

            migrationBuilder.DropColumn(
                name: "CampusId",
                table: "BusStations");
        }
    }
}
