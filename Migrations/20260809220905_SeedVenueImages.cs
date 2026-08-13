using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EventEaseBookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class SeedVenueImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Venues",
                columns: new[] { "VenueId", "Capacity", "ImageUrl", "Location", "VenueName" },
                values: new object[,]
                {
                    { 1, 500, "cityhall.png", "Downtown Johannesburg", "City Hall" },
                    { 2, 1200, "conferencecenter.png", "Sandton Business District", "Conference Center" },
                    { 3, 800, "grandhotelballroom.png", "Pretoria Central", "Grand Hotel Ballroom" },
                    { 4, 350, "communitypark.png", "Cape Town Waterfront", "Community Park" },
                    { 5, 5000, "sportsarena.png", "Soweto, Johannesburg", "Sports Arena" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "VenueId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "VenueId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "VenueId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "VenueId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "VenueId",
                keyValue: 5);
        }
    }
}
