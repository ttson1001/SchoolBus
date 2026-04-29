using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE_API.Migrations
{
    /// <inheritdoc />
    public partial class _428261119 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_BusStations_DropOffStationId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_BusStations_PickupStationId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_BusRuns_RouteId_ServiceDate_StartTime_TripType_RunOrder",
                table: "BusRuns");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_DropOffStationId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TripType",
                table: "BusRuns");

            migrationBuilder.DropColumn(
                name: "DropOffLatitude",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DropOffLongitude",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DropOffStationId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TripType",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "PickupStationId",
                table: "Bookings",
                newName: "StationId");

            migrationBuilder.RenameColumn(
                name: "PickupLongitude",
                table: "Bookings",
                newName: "Longitude");

            migrationBuilder.RenameColumn(
                name: "PickupLatitude",
                table: "Bookings",
                newName: "Latitude");

            migrationBuilder.RenameIndex(
                name: "IX_Bookings_PickupStationId",
                table: "Bookings",
                newName: "IX_Bookings_StationId");

            migrationBuilder.CreateIndex(
                name: "IX_BusRuns_RouteId_ServiceDate_StartTime_RunOrder",
                table: "BusRuns",
                columns: new[] { "RouteId", "ServiceDate", "StartTime", "RunOrder" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_BusStations_StationId",
                table: "Bookings",
                column: "StationId",
                principalTable: "BusStations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_BusStations_StationId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_BusRuns_RouteId_ServiceDate_StartTime_RunOrder",
                table: "BusRuns");

            migrationBuilder.RenameColumn(
                name: "StationId",
                table: "Bookings",
                newName: "PickupStationId");

            migrationBuilder.RenameColumn(
                name: "Longitude",
                table: "Bookings",
                newName: "PickupLongitude");

            migrationBuilder.RenameColumn(
                name: "Latitude",
                table: "Bookings",
                newName: "PickupLatitude");

            migrationBuilder.RenameIndex(
                name: "IX_Bookings_StationId",
                table: "Bookings",
                newName: "IX_Bookings_PickupStationId");

            migrationBuilder.AddColumn<string>(
                name: "TripType",
                table: "BusRuns",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "DropOffLatitude",
                table: "Bookings",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DropOffLongitude",
                table: "Bookings",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DropOffStationId",
                table: "Bookings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "TripType",
                table: "Bookings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_BusRuns_RouteId_ServiceDate_StartTime_TripType_RunOrder",
                table: "BusRuns",
                columns: new[] { "RouteId", "ServiceDate", "StartTime", "TripType", "RunOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_DropOffStationId",
                table: "Bookings",
                column: "DropOffStationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_BusStations_DropOffStationId",
                table: "Bookings",
                column: "DropOffStationId",
                principalTable: "BusStations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_BusStations_PickupStationId",
                table: "Bookings",
                column: "PickupStationId",
                principalTable: "BusStations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
