using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalTwin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledPrintJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scheduled_print_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    PreferredPrinterId = table.Column<Guid>(type: "uuid", nullable: true),
                    AllowAnyPrinter = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AssignedPrinterId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedPrinterTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedMaterialType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RequestedColor = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    EstimatedDurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    EstimatedFilamentGrams = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    RequestedStartAfterUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DueAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsSimulatedInput = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduled_print_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scheduled_print_jobs_printer_tasks_StartedPrinterTaskId",
                        column: x => x.StartedPrinterTaskId,
                        principalTable: "printer_tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_scheduled_print_jobs_printers_AssignedPrinterId",
                        column: x => x.AssignedPrinterId,
                        principalTable: "printers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_scheduled_print_jobs_printers_PreferredPrinterId",
                        column: x => x.PreferredPrinterId,
                        principalTable: "printers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_print_jobs_AssignedPrinterId",
                table: "scheduled_print_jobs",
                column: "AssignedPrinterId");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_print_jobs_CreatedAtUtc",
                table: "scheduled_print_jobs",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_print_jobs_DueAtUtc",
                table: "scheduled_print_jobs",
                column: "DueAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_print_jobs_PreferredPrinterId",
                table: "scheduled_print_jobs",
                column: "PreferredPrinterId");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_print_jobs_Priority",
                table: "scheduled_print_jobs",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_print_jobs_RequestedStartAfterUtc",
                table: "scheduled_print_jobs",
                column: "RequestedStartAfterUtc");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_print_jobs_StartedPrinterTaskId",
                table: "scheduled_print_jobs",
                column: "StartedPrinterTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_print_jobs_Status",
                table: "scheduled_print_jobs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scheduled_print_jobs");
        }
    }
}
