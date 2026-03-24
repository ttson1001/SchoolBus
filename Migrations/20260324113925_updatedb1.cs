using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE_API.Migrations
{
    /// <inheritdoc />
    public partial class updatedb1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DropOffStationId",
                table: "StudentBusAssignments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PickupStationId",
                table: "StudentBusAssignments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RideDate",
                table: "StudentBusAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CheckInStationId",
                table: "Attendances",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CheckOutStationId",
                table: "Attendances",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentBusAssignments_DropOffStationId",
                table: "StudentBusAssignments",
                column: "DropOffStationId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentBusAssignments_PickupStationId",
                table: "StudentBusAssignments",
                column: "PickupStationId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_CheckInStationId",
                table: "Attendances",
                column: "CheckInStationId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_CheckOutStationId",
                table: "Attendances",
                column: "CheckOutStationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_BusStations_CheckInStationId",
                table: "Attendances",
                column: "CheckInStationId",
                principalTable: "BusStations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_BusStations_CheckOutStationId",
                table: "Attendances",
                column: "CheckOutStationId",
                principalTable: "BusStations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentBusAssignments_BusStations_DropOffStationId",
                table: "StudentBusAssignments",
                column: "DropOffStationId",
                principalTable: "BusStations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentBusAssignments_BusStations_PickupStationId",
                table: "StudentBusAssignments",
                column: "PickupStationId",
                principalTable: "BusStations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_BusStations_CheckInStationId",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_BusStations_CheckOutStationId",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentBusAssignments_BusStations_DropOffStationId",
                table: "StudentBusAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentBusAssignments_BusStations_PickupStationId",
                table: "StudentBusAssignments");

            migrationBuilder.DropIndex(
                name: "IX_StudentBusAssignments_DropOffStationId",
                table: "StudentBusAssignments");

            migrationBuilder.DropIndex(
                name: "IX_StudentBusAssignments_PickupStationId",
                table: "StudentBusAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_CheckInStationId",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_CheckOutStationId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "DropOffStationId",
                table: "StudentBusAssignments");

            migrationBuilder.DropColumn(
                name: "PickupStationId",
                table: "StudentBusAssignments");

            migrationBuilder.DropColumn(
                name: "RideDate",
                table: "StudentBusAssignments");

            migrationBuilder.DropColumn(
                name: "CheckInStationId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "CheckOutStationId",
                table: "Attendances");
        }
    }
}
