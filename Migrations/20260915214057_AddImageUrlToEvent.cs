using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventEaseBookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrlToEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Events",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Events");
        }
    }
}
