using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE_API.Migrations
{
    /// <inheritdoc />
    public partial class _41920251219 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusTripProgresses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusId = table.Column<long>(type: "bigint", nullable: false),
                    BusScheduleId = table.Column<long>(type: "bigint", nullable: false),
                    RouteId = table.Column<long>(type: "bigint", nullable: false),
                    StationId = table.Column<long>(type: "bigint", nullable: false),
                    RideDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    ArrivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DepartedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusTripProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusTripProgresses_BusRoutes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "BusRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusTripProgresses_BusSchedules_BusScheduleId",
                        column: x => x.BusScheduleId,
                        principalTable: "BusSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusTripProgresses_BusStations_StationId",
                        column: x => x.StationId,
                        principalTable: "BusStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusTripProgresses_Buses_BusId",
                        column: x => x.BusId,
                        principalTable: "Buses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusTripProgresses_BusId",
                table: "BusTripProgresses",
                column: "BusId");

            migrationBuilder.CreateIndex(
                name: "IX_BusTripProgresses_BusScheduleId_RideDate_OrderIndex",
                table: "BusTripProgresses",
                columns: new[] { "BusScheduleId", "RideDate", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusTripProgresses_RouteId",
                table: "BusTripProgresses",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_BusTripProgresses_StationId",
                table: "BusTripProgresses",
                column: "StationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusTripProgresses");
        }
    }
}
