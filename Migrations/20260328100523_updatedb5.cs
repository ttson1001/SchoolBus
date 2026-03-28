using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE_API.Migrations
{
    /// <inheritdoc />
    public partial class updatedb5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "ConfidenceScore",
                table: "FaceRecognitionLogs",
                type: "decimal(10,6)",
                precision: 10,
                scale: 6,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "ConfidenceScore",
                table: "FaceRecognitionLogs",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,6)",
                oldPrecision: 10,
                oldScale: 6);
        }
    }
}
