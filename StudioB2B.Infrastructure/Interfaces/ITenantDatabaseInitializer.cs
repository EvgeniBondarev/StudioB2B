namespace StudioB2B.Infrastructure.Interfaces;

public interface ITenantDatabaseInitializer
{
    Task MigrateAndSeedAsync(string connectionString, CancellationToken ct);
    /// <summary>Применяет pending миграции и seed — для существующих тенантов при запуске приложения.</summary>
    Task MigrateOnlyAsync(string connectionString, CancellationToken ct);
    Task CreateAdminUserAsync(string connectionString, string email, string password, CancellationToken ct);
    Task DropDatabaseAsync(string connectionString, CancellationToken ct);
}
