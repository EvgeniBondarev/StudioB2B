using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioB2B.Infrastructure.Persistence.Tenant.Migrations
{
    /// <inheritdoc />
    public partial class AddOzonPushNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OzonPushNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MessageType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RawPayload = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SellerId = table.Column<long>(type: "bigint", nullable: true),
                    PostingNumber = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MarketplaceClientId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OzonPushNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OzonPushNotifications_MarketplaceClients_MarketplaceClientId",
                        column: x => x.MarketplaceClientId,
                        principalTable: "MarketplaceClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationTaskLogs_UserId",
                table: "CommunicationTaskLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OzonPushNotifications_MarketplaceClientId",
                table: "OzonPushNotifications",
                column: "MarketplaceClientId");

            migrationBuilder.CreateIndex(
                name: "IX_OzonPushNotifications_MessageType",
                table: "OzonPushNotifications",
                column: "MessageType");

            migrationBuilder.CreateIndex(
                name: "IX_OzonPushNotifications_ReceivedAtUtc",
                table: "OzonPushNotifications",
                column: "ReceivedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OzonPushNotifications_SellerId",
                table: "OzonPushNotifications",
                column: "SellerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OzonPushNotifications");

            migrationBuilder.DropIndex(
                name: "IX_CommunicationTaskLogs_UserId",
                table: "CommunicationTaskLogs");
        }
    }
}
