using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioB2B.Infrastructure.Persistence.Tenant.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunicationTaskBoard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommunicationPaymentRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TaskType = table.Column<int>(type: "int", nullable: false),
                    PaymentMode = table.Column<int>(type: "int", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationPaymentRates", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CommunicationTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TaskType = table.Column<int>(type: "int", nullable: false),
                    ExternalId = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MarketplaceClientId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    AssignedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    TotalTimeSpentTicks = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreviewText = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExternalStatus = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExternalUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PaymentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunicationTasks_MarketplaceClients_MarketplaceClientId",
                        column: x => x.MarketplaceClientId,
                        principalTable: "MarketplaceClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationTasks_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CommunicationTaskLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TaskId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Action = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Details = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationTaskLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunicationTaskLogs_CommunicationTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "CommunicationTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommunicationTaskLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CommunicationTimeEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TaskId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationTimeEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunicationTimeEntries_CommunicationTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "CommunicationTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommunicationTimeEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationPaymentRates_TaskType_IsActive",
                table: "CommunicationPaymentRates",
                columns: new[] { "TaskType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationTasks_TaskType_ExternalId_MarketplaceClientId",
                table: "CommunicationTasks",
                columns: new[] { "TaskType", "ExternalId", "MarketplaceClientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationTasks_Status",
                table: "CommunicationTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationTasks_AssignedToUserId",
                table: "CommunicationTasks",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationTasks_CreatedAt",
                table: "CommunicationTasks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationTasks_MarketplaceClientId",
                table: "CommunicationTasks",
                column: "MarketplaceClientId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationTaskLogs_TaskId",
                table: "CommunicationTaskLogs",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationTaskLogs_CreatedAt",
                table: "CommunicationTaskLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationTimeEntries_TaskId",
                table: "CommunicationTimeEntries",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationTimeEntries_UserId",
                table: "CommunicationTimeEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationTimeEntries_StartedAt",
                table: "CommunicationTimeEntries",
                column: "StartedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CommunicationTimeEntries");
            migrationBuilder.DropTable(name: "CommunicationTaskLogs");
            migrationBuilder.DropTable(name: "CommunicationTasks");
            migrationBuilder.DropTable(name: "CommunicationPaymentRates");
        }
    }
}
