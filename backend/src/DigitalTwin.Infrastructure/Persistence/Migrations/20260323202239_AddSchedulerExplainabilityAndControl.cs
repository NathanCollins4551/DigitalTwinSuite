using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalTwin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulerExplainabilityAndControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompatibilityNote",
                table: "scheduled_print_jobs",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EstimatedFinishAtUtc",
                table: "scheduled_print_jobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EstimatedStartAtUtc",
                table: "scheduled_print_jobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchedulerDecisionReason",
                table: "scheduled_print_jobs",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "scheduler_controls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPaused = table.Column<bool>(type: "boolean", nullable: false),
                    PauseReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduler_controls", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scheduler_controls");

            migrationBuilder.DropColumn(
                name: "CompatibilityNote",
                table: "scheduled_print_jobs");

            migrationBuilder.DropColumn(
                name: "EstimatedFinishAtUtc",
                table: "scheduled_print_jobs");

            migrationBuilder.DropColumn(
                name: "EstimatedStartAtUtc",
                table: "scheduled_print_jobs");

            migrationBuilder.DropColumn(
                name: "SchedulerDecisionReason",
                table: "scheduled_print_jobs");
        }
    }
}
