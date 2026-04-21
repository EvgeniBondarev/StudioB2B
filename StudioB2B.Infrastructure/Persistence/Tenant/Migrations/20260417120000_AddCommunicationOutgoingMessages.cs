using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioB2B.Infrastructure.Persistence.Tenant.Migrations;

public partial class AddCommunicationOutgoingMessages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CommunicationOutgoingMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", nullable: false),
                ExternalId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                TaskType = table.Column<int>(type: "int", nullable: false),
                ExternalMessageId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                SentByUserId = table.Column<Guid>(type: "char(36)", nullable: false),
                SentByUserName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                SentAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CommunicationOutgoingMessages", x => x.Id);
                table.ForeignKey(
                    name: "FK_CommunicationOutgoingMessages_Users_SentByUserId",
                    column: x => x.SentByUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_CommunicationOutgoingMessages_ExternalId_TaskType",
            table: "CommunicationOutgoingMessages",
            columns: new[] { "ExternalId", "TaskType" });

        migrationBuilder.CreateIndex(
            name: "IX_CommunicationOutgoingMessages_SentByUserId",
            table: "CommunicationOutgoingMessages",
            column: "SentByUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CommunicationOutgoingMessages");
    }
}

