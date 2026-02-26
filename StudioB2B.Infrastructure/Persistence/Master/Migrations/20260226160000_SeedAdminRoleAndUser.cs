using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioB2B.Infrastructure.Persistence.Master.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminRoleAndUser : Migration
    {
        private static readonly Guid RoleId = new("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        private static readonly Guid UserId = new("b2c3d4e5-f6a7-8901-bcde-f12345678901");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert role "Администратор"
            migrationBuilder.InsertData(
                table: "Roles",
                columns: ["Id", "Name", "IsDeleted"],
                values: [RoleId, "Администратор", false]);

            // Insert user demo@gmail.com
            migrationBuilder.InsertData(
                table: "Users",
                columns: ["Id", "Email", "PasswordHash", "IsDeleted"],
                values: [UserId, "demo@gmail.com", "$2a$11$A9Dp33vAQDIANSivU2bamu2rrFUAqjNLnBeJQurInxLgthJoOOZlq", false]);

            // Assign role to user (join table name from Initial migration: MasterRoleMasterUser)
            migrationBuilder.InsertData(
                table: "MasterRoleMasterUser",
                columns: ["RolesId", "UsersId"],
                values: [RoleId, UserId]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MasterRoleMasterUser",
                keyColumns: ["RolesId", "UsersId"],
                keyValues: [RoleId, UserId]);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: UserId);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: RoleId);
        }
    }
}

