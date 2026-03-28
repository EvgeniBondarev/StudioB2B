namespace StudioB2B.Infrastructure.Interfaces;

public interface ITenantDatabaseInitializer
{
    Task MigrateAndSeedAsync(string connectionString, string tenantSubdomain, CancellationToken ct);
    /// <summary>Применяет pending миграции и seed — для существующих тенантов при запуске приложения.</summary>
    Task MigrateOnlyAsync(string connectionString, string tenantSubdomain, CancellationToken ct);
    Task CreateAdminUserAsync(string connectionString, string email, string password,
        string firstName, string lastName, string? middleName, CancellationToken ct);
    Task DropDatabaseAsync(string connectionString, CancellationToken ct);
}
