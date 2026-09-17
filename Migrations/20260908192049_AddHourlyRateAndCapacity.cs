using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventEaseBookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddHourlyRateAndCapacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "Venues",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "Venues",
                keyColumn: "VenueId",
                keyValue: 1,
                column: "HourlyRate",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "Venues",
                keyColumn: "VenueId",
                keyValue: 2,
                column: "HourlyRate",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "Venues",
                keyColumn: "VenueId",
                keyValue: 3,
                column: "HourlyRate",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "Venues",
                keyColumn: "VenueId",
                keyValue: 4,
                column: "HourlyRate",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "Venues",
                keyColumn: "VenueId",
                keyValue: 5,
                column: "HourlyRate",
                value: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "Venues");
        }
    }
}
