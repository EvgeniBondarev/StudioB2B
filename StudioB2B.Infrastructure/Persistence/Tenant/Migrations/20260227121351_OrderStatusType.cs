using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioB2B.Infrastructure.Persistence.Tenant.Migrations
{
    /// <inheritdoc />
    public partial class OrderStatusType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsInternal",
                table: "OrderStatuses",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "MarketplaceClientTypeId",
                table: "OrderStatuses",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatuses_MarketplaceClientTypeId",
                table: "OrderStatuses",
                column: "MarketplaceClientTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderStatuses_MarketplaceClientTypes_MarketplaceClientTypeId",
                table: "OrderStatuses",
                column: "MarketplaceClientTypeId",
                principalTable: "MarketplaceClientTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderStatuses_MarketplaceClientTypes_MarketplaceClientTypeId",
                table: "OrderStatuses");

            migrationBuilder.DropIndex(
                name: "IX_OrderStatuses_MarketplaceClientTypeId",
                table: "OrderStatuses");

            migrationBuilder.DropColumn(
                name: "IsInternal",
                table: "OrderStatuses");

            migrationBuilder.DropColumn(
                name: "MarketplaceClientTypeId",
                table: "OrderStatuses");
        }
    }
}
