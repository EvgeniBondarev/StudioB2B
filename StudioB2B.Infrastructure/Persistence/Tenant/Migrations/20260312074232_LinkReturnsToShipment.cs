using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioB2B.Infrastructure.Persistence.Tenant.Migrations
{
    /// <inheritdoc />
    public partial class LinkReturnsToShipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasReturn",
                table: "Shipments",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ShipmentId",
                table: "OrderReturns",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_OrderReturns_ShipmentId",
                table: "OrderReturns",
                column: "ShipmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderReturns_Shipments_ShipmentId",
                table: "OrderReturns",
                column: "ShipmentId",
                principalTable: "Shipments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Data migration: populate ShipmentId from existing Order→Shipment links
            migrationBuilder.Sql(@"
                UPDATE OrderReturns r
                INNER JOIN Orders o ON r.OrderId = o.Id
                SET r.ShipmentId = o.ShipmentId
                WHERE r.OrderId IS NOT NULL;
            ");

            // Data migration: populate ShipmentId via PostingNumber for orphaned returns
            migrationBuilder.Sql(@"
                UPDATE OrderReturns r
                INNER JOIN Shipments s ON r.PostingNumber = s.PostingNumber
                SET r.ShipmentId = s.Id
                WHERE r.ShipmentId IS NULL AND r.PostingNumber IS NOT NULL;
            ");

            // Data migration: set Shipment.HasReturn from linked returns
            migrationBuilder.Sql(@"
                UPDATE Shipments s
                SET s.HasReturn = 1
                WHERE EXISTS (SELECT 1 FROM OrderReturns r WHERE r.ShipmentId = s.Id);
            ");

            migrationBuilder.DropColumn(
                name: "HasReturn",
                table: "Orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderReturns_Shipments_ShipmentId",
                table: "OrderReturns");

            migrationBuilder.DropIndex(
                name: "IX_OrderReturns_ShipmentId",
                table: "OrderReturns");

            migrationBuilder.DropColumn(
                name: "HasReturn",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "ShipmentId",
                table: "OrderReturns");

            migrationBuilder.AddColumn<bool>(
                name: "HasReturn",
                table: "Orders",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
