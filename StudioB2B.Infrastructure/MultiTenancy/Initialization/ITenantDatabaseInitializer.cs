namespace StudioB2B.Infrastructure.MultiTenancy.Initialization;

public interface ITenantDatabaseInitializer
{
    Task MigrateAndSeedAsync(string connectionString, CancellationToken ct);
    Task CreateAdminUserAsync(string connectionString, string email, string password, CancellationToken ct);
    Task DropDatabaseAsync(string connectionString, CancellationToken ct);
}
