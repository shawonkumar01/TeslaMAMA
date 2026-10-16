using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeslaMAMA.Migrations
{
    /// <inheritdoc />
    public partial class AllowUpToFiveSeats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_RideRequest_Seats",
                table: "RideRequests");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RideRequest_Seats",
                table: "RideRequests",
                sql: "\"Seats\" BETWEEN 1 AND 5");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_RideRequest_Seats",
                table: "RideRequests");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RideRequest_Seats",
                table: "RideRequests",
                sql: "\"Seats\" BETWEEN 1 AND 3");
        }
    }
}
