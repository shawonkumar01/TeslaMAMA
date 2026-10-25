using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeslaMAMA.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferAttemptsToRideRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OfferAttempts",
                table: "RideRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OfferAttempts",
                table: "RideRequests");
        }
    }
}
