using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioB2B.Infrastructure.Persistence.Tenant.Migrations
{
    /// <inheritdoc />
    public partial class AddOzonReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasReturn",
                table: "Orders",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "OrderReturns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),

                    OrderId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),

                    OzonReturnId = table.Column<long>(type: "bigint", nullable: false),
                    OzonOrderId = table.Column<long>(type: "bigint", nullable: true),
                    OrderNumber = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PostingNumber = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceId = table.Column<long>(type: "bigint", nullable: true),
                    ClearingId = table.Column<long>(type: "bigint", nullable: true),
                    ReturnClearingId = table.Column<long>(type: "bigint", nullable: true),

                    ReturnReasonName = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Schema = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),

                    ProductSku = table.Column<long>(type: "bigint", nullable: true),
                    OfferId = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProductName = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProductPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ProductPriceCurrencyCode = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProductPriceWithoutCommission = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    CommissionPercent = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    Commission = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ProductQuantity = table.Column<int>(type: "int", nullable: false),

                    VisualStatusId = table.Column<int>(type: "int", nullable: true),
                    VisualStatusDisplayName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VisualStatusSysName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VisualStatusChangeMoment = table.Column<DateTime>(type: "datetime(6)", nullable: true),

                    ReturnDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    TechnicalReturnMoment = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FinalMoment = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CancelledWithCompensationMoment = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LogisticBarcode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),

                    StorageSum = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    StorageCurrencyCode = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StorageTariffStartDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    StorageArrivedMoment = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    StorageDays = table.Column<long>(type: "bigint", nullable: true),
                    UtilizationSum = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    UtilizationForecastDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),

                    PlaceName = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PlaceAddress = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),

                    CompensationStatusId = table.Column<int>(type: "int", nullable: true),
                    CompensationStatusDisplayName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CompensationStatusChangeMoment = table.Column<DateTime>(type: "datetime(6)", nullable: true),

                    IsOpened = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsSuperEconom = table.Column<bool>(type: "tinyint(1)", nullable: false),

                    SyncedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderReturns_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_OrderReturns_OzonReturnId",
                table: "OrderReturns",
                column: "OzonReturnId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderReturns_OzonOrderId",
                table: "OrderReturns",
                column: "OzonOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderReturns_PostingNumber",
                table: "OrderReturns",
                column: "PostingNumber");

            migrationBuilder.CreateIndex(
                name: "IX_OrderReturns_VisualStatusSysName",
                table: "OrderReturns",
                column: "VisualStatusSysName");

            migrationBuilder.CreateIndex(
                name: "IX_OrderReturns_OrderId",
                table: "OrderReturns",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "OrderReturns");

            migrationBuilder.DropColumn(name: "HasReturn", table: "Orders");
        }
    }
}
