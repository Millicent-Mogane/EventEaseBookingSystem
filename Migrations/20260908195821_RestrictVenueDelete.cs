using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventEaseBookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class RestrictVenueDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Venues",
                keyColumn: "VenueId",
                keyValue: 1,
                columns: new[] { "HourlyRate", "ImageUrl" },
                values: new object[] { 800m, "/images/cityhall.png" });

            migrationBuilder.UpdateData(
                table: "Venues",
                keyColumn: "VenueId",
                keyValue: 2,
                columns: new[] { "HourlyRate", "ImageUrl" },
                values: new object[] { 1500m, "/images/conferencecenter.png" });

            migrationBuilder.UpdateData(
                table: "Venues",
                keyColumn: "VenueId",
                keyValue: 3,
                columns: new[] { "HourlyRate", "ImageUrl" },
                values: new object[] { 1200m, "/images/grandhotelballroom.png" });

            migrationBuilder.UpdateData(
                table: "Venues",
                keyColumn: "VenueId",
                keyValue: 4,
                columns: new[] { "HourlyRate", "ImageUrl" },
                values: new object[] { 500m, "/images/communitypark.png" });

            migrationBuilder.UpdateData(
                table: "Venues",
                keyColumn: "VenueId",
                keyValue: 5,
                columns: new[] { "HourlyRate", "ImageUrl" },
                values: new object[] { 2500m, "/images/sportsarena.png" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Venues",
                keyColumn: "VenueId",
                keyValue: 1,
                columns: new[] { "HourlyRate", "ImageUrl" },
                values: new object[] { 0m, "cityhall.png" });

            migrationBuilder.UpdateData(
                table: "Venues",
                keyColumn: "VenueId",
                keyValue: 2,
                columns: new[] { "HourlyRate", "ImageUrl" },
                values: new object[] { 0m, "conferencecenter.png" });

            migrationBuilder.UpdateData(
                table: "Venues",
                keyColumn: "VenueId",
                keyValue: 3,
                columns: new[] { "HourlyRate", "ImageUrl" },
                values: new object[] { 0m, "grandhotelballroom.png" });

            migrationBuilder.UpdateData(
                table: "Venues",
                keyColumn: "VenueId",
                keyValue: 4,
                columns: new[] { "HourlyRate", "ImageUrl" },
                values: new object[] { 0m, "communitypark.png" });

            migrationBuilder.UpdateData(
                table: "Venues",
                keyColumn: "VenueId",
                keyValue: 5,
                columns: new[] { "HourlyRate", "ImageUrl" },
                values: new object[] { 0m, "sportsarena.png" });
        }
    }
}
