using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE_API.Migrations
{
    /// <inheritdoc />
    public partial class updb2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusAssignments_BusSchedules_BusScheduleId",
                table: "BusAssignments");

            migrationBuilder.RenameColumn(
                name: "BusScheduleId",
                table: "BusAssignments",
                newName: "BusId");

            migrationBuilder.RenameIndex(
                name: "IX_BusAssignments_BusScheduleId",
                table: "BusAssignments",
                newName: "IX_BusAssignments_BusId");

            migrationBuilder.AddForeignKey(
                name: "FK_BusAssignments_Buses_BusId",
                table: "BusAssignments",
                column: "BusId",
                principalTable: "Buses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusAssignments_Buses_BusId",
                table: "BusAssignments");

            migrationBuilder.RenameColumn(
                name: "BusId",
                table: "BusAssignments",
                newName: "BusScheduleId");

            migrationBuilder.RenameIndex(
                name: "IX_BusAssignments_BusId",
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
    }
}
