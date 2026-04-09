using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioB2B.Infrastructure.Persistence.Master.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            // Existing users were registered before verification was introduced — mark them as verified.
            migrationBuilder.Sql("UPDATE Users SET IsEmailVerified = 1 WHERE IsEmailVerified = 0;");

            migrationBuilder.CreateTable(
                name: "EmailVerificationCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Code = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsUsed = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailVerificationCodes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationCodes_Email",
                table: "EmailVerificationCodes",
                column: "Email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailVerificationCodes");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "Users");
        }
    }
}
