using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalTwin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCvZoneSpoolsAndPrinterLoadedSpools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cv_zone_spools",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CameraId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ZoneName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SpoolCode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MaterialType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ColorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ColorHex = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LastSeenAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cv_zone_spools", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "printer_loaded_spools",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrinterId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrinterAmsUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    SlotIndex = table.Column<int>(type: "integer", nullable: false),
                    SpoolCode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MaterialType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ColorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ColorHex = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RemainingPercent = table.Column<decimal>(type: "numeric", nullable: true),
                    RemainingGrams = table.Column<decimal>(type: "numeric", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_printer_loaded_spools", x => x.Id);
                    table.ForeignKey(
                        name: "FK_printer_loaded_spools_printer_ams_units_PrinterAmsUnitId",
                        column: x => x.PrinterAmsUnitId,
                        principalTable: "printer_ams_units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_printer_loaded_spools_printers_PrinterId",
                        column: x => x.PrinterId,
                        principalTable: "printers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cv_zone_spools_CameraId_ZoneName_SpoolCode",
                table: "cv_zone_spools",
                columns: new[] { "CameraId", "ZoneName", "SpoolCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cv_zone_spools_MaterialType_ColorName",
                table: "cv_zone_spools",
                columns: new[] { "MaterialType", "ColorName" });

            migrationBuilder.CreateIndex(
                name: "IX_printer_loaded_spools_PrinterAmsUnitId",
                table: "printer_loaded_spools",
                column: "PrinterAmsUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_printer_loaded_spools_PrinterId_SlotIndex_IsActive",
                table: "printer_loaded_spools",
                columns: new[] { "PrinterId", "SlotIndex", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cv_zone_spools");

            migrationBuilder.DropTable(
                name: "printer_loaded_spools");
        }
    }
}
