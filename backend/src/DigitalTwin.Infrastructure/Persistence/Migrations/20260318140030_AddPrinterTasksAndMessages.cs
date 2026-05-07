using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalTwin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrinterTasksAndMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "printer_tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalTaskId = table.Column<long>(type: "bigint", nullable: false),
                    PrinterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeviceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DeviceName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeviceModel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DesignTitle = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DesignTitleTranslated = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    StatusText = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FailedType = table.Column<int>(type: "integer", nullable: true),
                    Mode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BedType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CostTimeSeconds = table.Column<int>(type: "integer", nullable: true),
                    LengthMm = table.Column<int>(type: "integer", nullable: true),
                    WeightGrams = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    StartTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CoverUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    RawJson = table.Column<string>(type: "text", nullable: false),
                    SourceUpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_printer_tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_printer_tasks_printers_PrinterId",
                        column: x => x.PrinterId,
                        principalTable: "printers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "printer_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalMessageId = table.Column<long>(type: "bigint", nullable: false),
                    PrinterId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalTaskId = table.Column<long>(type: "bigint", nullable: true),
                    RelatedPrinterTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: true),
                    IsRead = table.Column<int>(type: "integer", nullable: true),
                    CreateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeviceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DeviceName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TaskStatus = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Detail = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CoverUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DesignId = table.Column<int>(type: "integer", nullable: true),
                    DesignTitle = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    RawJson = table.Column<string>(type: "text", nullable: false),
                    SourceUpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_printer_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_printer_messages_printer_tasks_RelatedPrinterTaskId",
                        column: x => x.RelatedPrinterTaskId,
                        principalTable: "printer_tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_printer_messages_printers_PrinterId",
                        column: x => x.PrinterId,
                        principalTable: "printers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "printer_task_ams_details",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrinterTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ams = table.Column<int>(type: "integer", nullable: true),
                    AmsId = table.Column<int>(type: "integer", nullable: true),
                    SlotId = table.Column<int>(type: "integer", nullable: true),
                    NozzleId = table.Column<int>(type: "integer", nullable: true),
                    FilamentId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    FilamentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TargetFilamentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SourceColor = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    TargetColor = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    WeightGrams = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_printer_task_ams_details", x => x.Id);
                    table.ForeignKey(
                        name: "FK_printer_task_ams_details_printer_tasks_PrinterTaskId",
                        column: x => x.PrinterTaskId,
                        principalTable: "printer_tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_printer_messages_DeviceId",
                table: "printer_messages",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_printer_messages_ExternalMessageId",
                table: "printer_messages",
                column: "ExternalMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_printer_messages_ExternalTaskId",
                table: "printer_messages",
                column: "ExternalTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_printer_messages_PrinterId",
                table: "printer_messages",
                column: "PrinterId");

            migrationBuilder.CreateIndex(
                name: "IX_printer_messages_RelatedPrinterTaskId",
                table: "printer_messages",
                column: "RelatedPrinterTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_printer_task_ams_details_PrinterTaskId_Ams_AmsId_SlotId_Noz~",
                table: "printer_task_ams_details",
                columns: new[] { "PrinterTaskId", "Ams", "AmsId", "SlotId", "NozzleId" });

            migrationBuilder.CreateIndex(
                name: "IX_printer_tasks_DeviceId",
                table: "printer_tasks",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_printer_tasks_ExternalTaskId",
                table: "printer_tasks",
                column: "ExternalTaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_printer_tasks_PrinterId",
                table: "printer_tasks",
                column: "PrinterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "printer_messages");

            migrationBuilder.DropTable(
                name: "printer_task_ams_details");

            migrationBuilder.DropTable(
                name: "printer_tasks");
        }
    }
}
