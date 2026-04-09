using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioB2B.Infrastructure.Persistence.Tenant.Migrations;

public partial class AddWasPreviouslyCompleted : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "WasPreviouslyCompleted",
            table: "CommunicationTasks",
            type: "tinyint(1)",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "WasPreviouslyCompleted",
            table: "CommunicationTasks");
    }
}
