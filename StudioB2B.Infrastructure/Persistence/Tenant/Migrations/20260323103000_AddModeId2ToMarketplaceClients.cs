using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioB2B.Infrastructure.Persistence.Tenant.Migrations
{
    /// <inheritdoc />
    public partial class AddModeId2ToMarketplaceClients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ModeId2",
                table: "MarketplaceClients",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceClients_ModeId2",
                table: "MarketplaceClients",
                column: "ModeId2");

            migrationBuilder.AddForeignKey(
                name: "FK_MarketplaceClients_MarketplaceClientModes_ModeId2",
                table: "MarketplaceClients",
                column: "ModeId2",
                principalTable: "MarketplaceClientModes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MarketplaceClients_MarketplaceClientModes_ModeId2",
                table: "MarketplaceClients");

            migrationBuilder.DropIndex(
                name: "IX_MarketplaceClients_ModeId2",
                table: "MarketplaceClients");

            migrationBuilder.DropColumn(
                name: "ModeId2",
                table: "MarketplaceClients");
        }
    }
}

