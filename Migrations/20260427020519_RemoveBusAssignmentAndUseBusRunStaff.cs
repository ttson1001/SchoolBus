using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE_API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBusAssignmentAndUseBusRunStaff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusAssignments");

            migrationBuilder.AddColumn<long>(
                name: "DriverId",
                table: "BusRuns",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TeacherId",
                table: "BusRuns",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusRuns_DriverId",
                table: "BusRuns",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_BusRuns_TeacherId",
                table: "BusRuns",
                column: "TeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_BusRuns_Users_DriverId",
                table: "BusRuns",
                column: "DriverId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BusRuns_Users_TeacherId",
                table: "BusRuns",
                column: "TeacherId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusRuns_Users_DriverId",
                table: "BusRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_BusRuns_Users_TeacherId",
                table: "BusRuns");

            migrationBuilder.DropIndex(
                name: "IX_BusRuns_DriverId",
                table: "BusRuns");

            migrationBuilder.DropIndex(
                name: "IX_BusRuns_TeacherId",
                table: "BusRuns");

            migrationBuilder.DropColumn(
                name: "DriverId",
                table: "BusRuns");

            migrationBuilder.DropColumn(
                name: "TeacherId",
                table: "BusRuns");

            migrationBuilder.CreateTable(
                name: "BusAssignments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusId = table.Column<long>(type: "bigint", nullable: false),
                    DriverId = table.Column<long>(type: "bigint", nullable: false),
                    TeacherId = table.Column<long>(type: "bigint", nullable: false),
                    ActiveDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusAssignments_Buses_BusId",
                        column: x => x.BusId,
                        principalTable: "Buses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusAssignments_Users_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusAssignments_Users_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusAssignments_BusId_ActiveDate",
                table: "BusAssignments",
                columns: new[] { "BusId", "ActiveDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusAssignments_DriverId",
                table: "BusAssignments",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_BusAssignments_TeacherId",
                table: "BusAssignments",
                column: "TeacherId");
        }
    }
}
