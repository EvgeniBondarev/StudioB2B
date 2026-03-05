using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioB2B.Infrastructure.Persistence.Tenant.Migrations
{
    /// <inheritdoc />
    public partial class ExtendSyncJobSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CronExpression",
                table: "SyncJobSchedules",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DaysOfWeek",
                table: "SyncJobSchedules",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "IntervalDays",
                table: "SyncJobSchedules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IntervalHours",
                table: "SyncJobSchedules",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CronExpression",
                table: "SyncJobSchedules");

            migrationBuilder.DropColumn(
                name: "DaysOfWeek",
                table: "SyncJobSchedules");

            migrationBuilder.DropColumn(
                name: "IntervalDays",
                table: "SyncJobSchedules");

            migrationBuilder.DropColumn(
                name: "IntervalHours",
                table: "SyncJobSchedules");
        }
    }
}
