using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioB2B.Infrastructure.Persistence.Master.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantRestoreHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantRestoreHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SourceObjectKey = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErrorMessage = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantRestoreHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantRestoreHistories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRestoreHistories_StartedAtUtc",
                table: "TenantRestoreHistories",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRestoreHistories_TenantId",
                table: "TenantRestoreHistories",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantRestoreHistories");
        }
    }
}
