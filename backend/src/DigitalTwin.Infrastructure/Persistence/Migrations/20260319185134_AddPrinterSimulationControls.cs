using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalTwin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrinterSimulationControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSimulated",
                table: "printer_tasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SimulatedCompleteAtUtc",
                table: "printer_tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "printer_simulation_controls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrinterId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    LockedUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SimulationState = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ActivePrinterTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_printer_simulation_controls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_printer_simulation_controls_printers_PrinterId",
                        column: x => x.PrinterId,
                        principalTable: "printers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_printer_simulation_controls_IsLocked",
                table: "printer_simulation_controls",
                column: "IsLocked");

            migrationBuilder.CreateIndex(
                name: "IX_printer_simulation_controls_LockedUntilUtc",
                table: "printer_simulation_controls",
                column: "LockedUntilUtc");

            migrationBuilder.CreateIndex(
                name: "IX_printer_simulation_controls_PrinterId",
                table: "printer_simulation_controls",
                column: "PrinterId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "printer_simulation_controls");

            migrationBuilder.DropColumn(
                name: "IsSimulated",
                table: "printer_tasks");

            migrationBuilder.DropColumn(
                name: "SimulatedCompleteAtUtc",
                table: "printer_tasks");
        }
    }
}
