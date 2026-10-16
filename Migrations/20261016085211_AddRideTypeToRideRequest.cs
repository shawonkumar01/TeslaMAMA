using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeslaMAMA.Migrations
{
    /// <inheritdoc />
    public partial class AddRideTypeToRideRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RideType",
                table: "RideRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RideType",
                table: "RideRequests");
        }
    }
}
