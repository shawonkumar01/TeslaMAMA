using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TeslaMAMA.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverOffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DriverOffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RideRequestId = table.Column<int>(type: "integer", nullable: false),
                    VehicleId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverOffers_RideRequests_RideRequestId",
                        column: x => x.RideRequestId,
                        principalTable: "RideRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DriverOffers_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverOffers_RideRequestId_VehicleId",
                table: "DriverOffers",
                columns: new[] { "RideRequestId", "VehicleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverOffers_VehicleId_Status",
                table: "DriverOffers",
                columns: new[] { "VehicleId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverOffers");
        }
    }
}
