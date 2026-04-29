using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE_API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBusScheduleAndUseBusRunSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusTripProgresses_BusSchedules_BusScheduleId",
                table: "BusTripProgresses");

            migrationBuilder.DropTable(
                name: "BusSchedules");

            migrationBuilder.RenameColumn(
                name: "BusScheduleId",
                table: "BusTripProgresses",
                newName: "BusRunId");

            migrationBuilder.RenameIndex(
                name: "IX_BusTripProgresses_BusScheduleId_RideDate_OrderIndex",
                table: "BusTripProgresses",
                newName: "IX_BusTripProgresses_BusRunId_RideDate_OrderIndex");

            migrationBuilder.AddForeignKey(
                name: "FK_BusTripProgresses_BusRuns_BusRunId",
                table: "BusTripProgresses",
                column: "BusRunId",
                principalTable: "BusRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusTripProgresses_BusRuns_BusRunId",
                table: "BusTripProgresses");

            migrationBuilder.RenameColumn(
                name: "BusRunId",
                table: "BusTripProgresses",
                newName: "BusScheduleId");

            migrationBuilder.RenameIndex(
                name: "IX_BusTripProgresses_BusRunId_RideDate_OrderIndex",
                table: "BusTripProgresses",
                newName: "IX_BusTripProgresses_BusScheduleId_RideDate_OrderIndex");

            migrationBuilder.CreateTable(
                name: "BusSchedules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusId = table.Column<long>(type: "bigint", nullable: false),
                    RouteId = table.Column<long>(type: "bigint", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ScheduleType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShiftType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusSchedules_BusRoutes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "BusRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusSchedules_Buses_BusId",
                        column: x => x.BusId,
                        principalTable: "Buses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusSchedules_BusId",
                table: "BusSchedules",
                column: "BusId");

            migrationBuilder.CreateIndex(
                name: "IX_BusSchedules_RouteId",
                table: "BusSchedules",
                column: "RouteId");

            migrationBuilder.AddForeignKey(
                name: "FK_BusTripProgresses_BusSchedules_BusScheduleId",
                table: "BusTripProgresses",
                column: "BusScheduleId",
                principalTable: "BusSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
