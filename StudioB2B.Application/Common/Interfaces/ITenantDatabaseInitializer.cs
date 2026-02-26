namespace StudioB2B.Application.Common.Interfaces;

public interface ITenantDatabaseInitializer
{
    Task MigrateAndSeedAsync(string connectionString, string email, string password,
        string surname, string firstName, string patronymic, CancellationToken ct);
    Task DropDatabaseAsync(string connectionString, CancellationToken ct);
}
