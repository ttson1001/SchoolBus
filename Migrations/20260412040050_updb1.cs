using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE_API.Migrations
{
    /// <inheritdoc />
    public partial class updb1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentBusAssignments_Buses_BusId",
                table: "StudentBusAssignments");

            migrationBuilder.DropIndex(
                name: "IX_StudentBusAssignments_BusId",
                table: "StudentBusAssignments");

            migrationBuilder.DropColumn(
                name: "BusId",
                table: "StudentBusAssignments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BusId",
                table: "StudentBusAssignments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_StudentBusAssignments_BusId",
                table: "StudentBusAssignments",
                column: "BusId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentBusAssignments_Buses_BusId",
                table: "StudentBusAssignments",
                column: "BusId",
                principalTable: "Buses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
