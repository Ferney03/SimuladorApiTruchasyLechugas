using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AquacultureAPI.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LechugasData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TiempoSegundos = table.Column<int>(type: "int", nullable: false),
                    AlturaCm = table.Column<double>(type: "float(10)", precision: 10, scale: 4, nullable: false),
                    AreaFoliarCm2 = table.Column<double>(type: "float(10)", precision: 10, scale: 4, nullable: false),
                    TemperaturaC = table.Column<double>(type: "float(10)", precision: 10, scale: 4, nullable: false),
                    HumedadPorcentaje = table.Column<double>(type: "float(10)", precision: 10, scale: 4, nullable: false),
                    pH = table.Column<double>(type: "float(10)", precision: 10, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LechugasData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TruchasData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TiempoSegundos = table.Column<int>(type: "int", nullable: false),
                    LongitudCm = table.Column<double>(type: "float(10)", precision: 10, scale: 4, nullable: false),
                    TemperaturaC = table.Column<double>(type: "float(10)", precision: 10, scale: 4, nullable: false),
                    ConductividadUsCm = table.Column<double>(type: "float(10)", precision: 10, scale: 4, nullable: false),
                    pH = table.Column<double>(type: "float(10)", precision: 10, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TruchasData", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LechugasData_TiempoSegundos",
                table: "LechugasData",
                column: "TiempoSegundos");

            migrationBuilder.CreateIndex(
                name: "IX_LechugasData_Timestamp",
                table: "LechugasData",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_TruchasData_TiempoSegundos",
                table: "TruchasData",
                column: "TiempoSegundos");

            migrationBuilder.CreateIndex(
                name: "IX_TruchasData_Timestamp",
                table: "TruchasData",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LechugasData");

            migrationBuilder.DropTable(
                name: "TruchasData");
        }
    }
}
