using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioB2B.Infrastructure.Persistence.Tenant.Migrations;

public partial class AddPaymentRateDurationTiers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "MinDurationMinutes",
            table: "CommunicationPaymentRates",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "MaxDurationMinutes",
            table: "CommunicationPaymentRates",
            type: "int",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "MinDurationMinutes", table: "CommunicationPaymentRates");
        migrationBuilder.DropColumn(name: "MaxDurationMinutes", table: "CommunicationPaymentRates");
    }
}
