using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioB2B.Infrastructure.Persistence.Tenant.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderTransactionFieldRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderTransactionFieldRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    OrderTransactionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EntityPath = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValueSource = table.Column<int>(type: "int", nullable: false),
                    FixedValue = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderTransactionFieldRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderTransactionFieldRules_OrderTransactions_OrderTransactionId",
                        column: x => x.OrderTransactionId,
                        principalTable: "OrderTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_OrderTransactionFieldRules_OrderTransactionId",
                table: "OrderTransactionFieldRules",
                column: "OrderTransactionId");

            migrationBuilder.AddColumn<int>(
                name: "FieldsUpdated",
                table: "OrderTransactionHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FieldsUpdated",
                table: "OrderTransactionHistories");

            migrationBuilder.DropTable(
                name: "OrderTransactionFieldRules");
        }
    }
}
