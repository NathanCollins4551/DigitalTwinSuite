using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalTwin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemovePrinterAmsSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "printer_ams_slots");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "printer_ams_slots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrinterAmsUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NozzleTempMaxC = table.Column<int>(type: "integer", nullable: true),
                    NozzleTempMinC = table.Column<int>(type: "integer", nullable: true),
                    RemainingLengthMm = table.Column<int>(type: "integer", nullable: true),
                    SlotIndex = table.Column<int>(type: "integer", nullable: false),
                    TotalLengthMm = table.Column<int>(type: "integer", nullable: true),
                    TrayColor = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    TrayType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
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
        }
    }
}
