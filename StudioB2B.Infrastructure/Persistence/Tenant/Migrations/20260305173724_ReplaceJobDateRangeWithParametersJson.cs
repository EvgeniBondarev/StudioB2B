using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioB2B.Infrastructure.Persistence.Tenant.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceJobDateRangeWithParametersJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateFrom",
                table: "SyncJobHistories");

            migrationBuilder.DropColumn(
                name: "DateTo",
                table: "SyncJobHistories");

            migrationBuilder.AddColumn<string>(
                name: "ParametersJson",
                table: "SyncJobHistories",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParametersJson",
                table: "SyncJobHistories");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateFrom",
                table: "SyncJobHistories",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateTo",
                table: "SyncJobHistories",
                type: "datetime(6)",
                nullable: true);
        }
    }
}
