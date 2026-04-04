using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE_API.Migrations
{
    /// <inheritdoc />
    public partial class Updatedb18 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusAssignments_BusRoutes_RouteId",
                table: "BusAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_BusAssignments_Buses_BusId",
                table: "BusAssignments");

            migrationBuilder.DropIndex(
                name: "IX_BusAssignments_BusId",
                table: "BusAssignments");

            migrationBuilder.DropColumn(
                name: "BusId",
                table: "BusAssignments");

            migrationBuilder.RenameColumn(
                name: "RouteId",
                table: "BusAssignments",
                newName: "BusScheduleId");

            migrationBuilder.RenameIndex(
                name: "IX_BusAssignments_RouteId",
                table: "BusAssignments",
                newName: "IX_BusAssignments_BusScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_BusAssignments_BusSchedules_BusScheduleId",
                table: "BusAssignments",
                column: "BusScheduleId",
                principalTable: "BusSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusAssignments_BusSchedules_BusScheduleId",
                table: "BusAssignments");

            migrationBuilder.RenameColumn(
                name: "BusScheduleId",
                table: "BusAssignments",
                newName: "RouteId");

            migrationBuilder.RenameIndex(
                name: "IX_BusAssignments_BusScheduleId",
                table: "BusAssignments",
                newName: "IX_BusAssignments_RouteId");

            migrationBuilder.AddColumn<long>(
                name: "BusId",
                table: "BusAssignments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_BusAssignments_BusId",
                table: "BusAssignments",
                column: "BusId");

            migrationBuilder.AddForeignKey(
                name: "FK_BusAssignments_BusRoutes_RouteId",
                table: "BusAssignments",
                column: "RouteId",
                principalTable: "BusRoutes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BusAssignments_Buses_BusId",
                table: "BusAssignments",
                column: "BusId",
                principalTable: "Buses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
