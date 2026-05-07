using DigitalTwin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalTwin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DigitalTwinDbContext))]
    [Migration("20260422120000_AddPrinterTaskAlias")]
    public partial class AddPrinterTaskAlias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TaskAlias",
                table: "printer_tasks",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "printer_tasks"
                SET "TaskAlias" = CONCAT(
                    'PT-',
                    COALESCE(
                        NULLIF(
                            TRIM(BOTH '-' FROM LEFT(
                                TRIM(BOTH '-' FROM REGEXP_REPLACE(
                                    UPPER(COALESCE(NULLIF("DeviceName", ''), NULLIF("DeviceId", ''), 'PRINTER')),
                                    '[^A-Z0-9]+',
                                    '-',
                                    'g'
                                )),
                                24
                            )),
                            ''
                        ),
                        'PRINTER'
                    ),
                    '-',
                    UPPER(TO_HEX("ExternalTaskId"))
                )
                WHERE COALESCE("TaskAlias", '') = '';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "TaskAlias",
                table: "printer_tasks",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_printer_tasks_TaskAlias",
                table: "printer_tasks",
                column: "TaskAlias",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_printer_tasks_TaskAlias",
                table: "printer_tasks");

            migrationBuilder.DropColumn(
                name: "TaskAlias",
                table: "printer_tasks");
        }
    }
}
