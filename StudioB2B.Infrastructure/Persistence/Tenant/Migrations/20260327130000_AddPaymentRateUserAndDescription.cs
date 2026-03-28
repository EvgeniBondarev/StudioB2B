using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioB2B.Infrastructure.Persistence.Tenant.Migrations;

public partial class AddPaymentRateUserAndDescription : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "TaskType",
            table: "CommunicationPaymentRates",
            type: "int",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "int");

        migrationBuilder.AddColumn<Guid>(
            name: "UserId",
            table: "CommunicationPaymentRates",
            type: "char(36)",
            nullable: true,
            collation: "ascii_general_ci");

        migrationBuilder.AddColumn<string>(
            name: "Description",
            table: "CommunicationPaymentRates",
            type: "varchar(256)",
            maxLength: 256,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_CommunicationPaymentRates_UserId",
            table: "CommunicationPaymentRates",
            column: "UserId");

        migrationBuilder.AddForeignKey(
            name: "FK_CommunicationPaymentRates_Users_UserId",
            table: "CommunicationPaymentRates",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_CommunicationPaymentRates_Users_UserId",
            table: "CommunicationPaymentRates");

        migrationBuilder.DropIndex(
            name: "IX_CommunicationPaymentRates_UserId",
            table: "CommunicationPaymentRates");

        migrationBuilder.DropColumn(name: "UserId", table: "CommunicationPaymentRates");
        migrationBuilder.DropColumn(name: "Description", table: "CommunicationPaymentRates");

        migrationBuilder.AlterColumn<int>(
            name: "TaskType",
            table: "CommunicationPaymentRates",
            type: "int",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);
    }
}
