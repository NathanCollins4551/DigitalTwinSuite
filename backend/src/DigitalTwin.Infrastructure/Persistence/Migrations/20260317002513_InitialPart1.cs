using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalTwin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPart1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "printers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsOnline = table.Column<bool>(type: "boolean", nullable: false),
                    PrintStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ModelName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ProductName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Structure = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    NozzleDiameterMm = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    IsAmsSupported = table.Column<bool>(type: "boolean", nullable: false),
                    LastBindSyncAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_printers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "printer_ams_units",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrinterId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmsIndex = table.Column<int>(type: "integer", nullable: false),
                    AmsDeviceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CurrentVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LatestVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ForceUpdate = table.Column<bool>(type: "boolean", nullable: true),
                    ReleaseStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DownloadUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    HumidityLevel = table.Column<int>(type: "integer", nullable: true),
                    TemperatureCelsius = table.Column<decimal>(type: "numeric", nullable: true),
                    LastSeenAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_printer_ams_units", x => x.Id);
                    table.ForeignKey(
                        name: "FK_printer_ams_units_printers_PrinterId",
                        column: x => x.PrinterId,
                        principalTable: "printers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "printer_firmware_statuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrinterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LatestVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ForceUpdate = table.Column<bool>(type: "boolean", nullable: true),
                    ReleaseStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DownloadUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    LastVersionSyncAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_printer_firmware_statuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_printer_firmware_statuses_printers_PrinterId",
                        column: x => x.PrinterId,
                        principalTable: "printers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "printer_ams_slots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrinterAmsUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotIndex = table.Column<int>(type: "integer", nullable: false),
                    TrayType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TrayColor = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    RemainingLengthMm = table.Column<int>(type: "integer", nullable: true),
                    TotalLengthMm = table.Column<int>(type: "integer", nullable: true),
                    NozzleTempMinC = table.Column<int>(type: "integer", nullable: true),
                    NozzleTempMaxC = table.Column<int>(type: "integer", nullable: true),
                    LastSeenAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_printer_ams_slots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_printer_ams_slots_printer_ams_units_PrinterAmsUnitId",
                        column: x => x.PrinterAmsUnitId,
                        principalTable: "printer_ams_units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_printer_ams_slots_PrinterAmsUnitId_SlotIndex",
                table: "printer_ams_slots",
                columns: new[] { "PrinterAmsUnitId", "SlotIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_printer_ams_units_PrinterId_AmsIndex",
                table: "printer_ams_units",
                columns: new[] { "PrinterId", "AmsIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_printer_firmware_statuses_PrinterId",
                table: "printer_firmware_statuses",
                column: "PrinterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_printers_DeviceId",
                table: "printers",
                column: "DeviceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "printer_ams_slots");

            migrationBuilder.DropTable(
                name: "printer_firmware_statuses");

            migrationBuilder.DropTable(
                name: "printer_ams_units");

            migrationBuilder.DropTable(
                name: "printers");
        }
    }
}
