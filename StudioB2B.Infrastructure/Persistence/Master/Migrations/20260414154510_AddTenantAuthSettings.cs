using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioB2B.Infrastructure.Persistence.Master.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantAuthSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequireEmailActivation",
                table: "Tenants",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequireLoginCode",
                table: "Tenants",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequireEmailActivation",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "RequireLoginCode",
                table: "Tenants");
        }
    }
}
