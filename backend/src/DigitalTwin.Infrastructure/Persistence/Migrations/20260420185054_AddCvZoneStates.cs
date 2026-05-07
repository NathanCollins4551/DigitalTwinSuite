using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalTwin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCvZoneStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cv_zone_states",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CameraId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ZoneName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SpoolIdsJson = table.Column<string>(type: "text", nullable: false),
                    UnknownSpoolCount = table.Column<int>(type: "integer", nullable: false),
                    OtherObjectCount = table.Column<int>(type: "integer", nullable: false),
                    LastInventoryTsUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastAnomalyTsUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cv_zone_states", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cv_zone_states_CameraId_ZoneName",
                table: "cv_zone_states",
                columns: new[] { "CameraId", "ZoneName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cv_zone_states");
        }
    }
}
