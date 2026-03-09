using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioB2B.Infrastructure.Persistence.Tenant.Migrations
{
    /// <inheritdoc />
    public partial class AddColorToOrderTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: add column only if it doesn't exist (fixes "Duplicate column" when migration was partially applied)
            migrationBuilder.Sql(@"
                SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'OrderTransactions'
                    AND COLUMN_NAME = 'Color');
                SET @ddl = IF(@col_exists = 0,
                    'ALTER TABLE `OrderTransactions` ADD COLUMN `Color` varchar(50) NULL CHARACTER SET utf8mb4',
                    'SELECT 1');
                PREPARE stmt FROM @ddl;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'OrderTransactions'
                    AND COLUMN_NAME = 'Color');
                SET @ddl = IF(@col_exists > 0,
                    'ALTER TABLE `OrderTransactions` DROP COLUMN `Color`',
                    'SELECT 1');
                PREPARE stmt FROM @ddl;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");
        }
    }
}
