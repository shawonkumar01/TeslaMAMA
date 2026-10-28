using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TeslaMAMA.Migrations
{
    /// <inheritdoc />
    public partial class AddRoadDistances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoadDistances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FromZone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ToZone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Km = table.Column<double>(type: "double precision", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadDistances", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoadDistances_FromZone_ToZone",
                table: "RoadDistances",
                columns: new[] { "FromZone", "ToZone" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoadDistances");
        }
    }
}
