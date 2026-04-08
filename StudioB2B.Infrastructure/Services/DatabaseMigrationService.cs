using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Master;

namespace StudioB2B.Infrastructure.Services;

/// <summary>
/// Сервис автоматического применения миграций при запуске (IHostedService)
/// </summary>
public class DatabaseMigrationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseMigrationService> _logger;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public DatabaseMigrationService(IServiceProvider serviceProvider, ILogger<DatabaseMigrationService> logger,
                                    IHostEnvironment environment, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _environment = environment;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {

        var connectionString = _configuration.GetConnectionString("MasterDb") ?? "";
        _logger.LogInformation("Environment: {Environment}, Database: {Database}",
            _environment.EnvironmentName,
            connectionString.Contains("Database=")
                ? connectionString.Split("Database=")[1].Split(";")[0]
                : "unknown");

        _logger.LogInformation("Applying database migrations...");

        try
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var masterDbContext = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

            var appliedMigrations = await masterDbContext.Database
                .GetAppliedMigrationsAsync(cancellationToken);

            _logger.LogInformation("Applied master migrations: {Migrations}",
                appliedMigrations.Any() ? string.Join(", ", appliedMigrations) : "<none>");

            var pendingMigrations = await masterDbContext.Database
                .GetPendingMigrationsAsync(cancellationToken);

            var migrations = pendingMigrations.ToList();
            if (migrations.Count > 0)
            {
                _logger.LogInformation("Found {Count} pending migrations: {Migrations}",
                    migrations.Count, string.Join(", ", migrations));

                await masterDbContext.Database.MigrateAsync(cancellationToken);

                _logger.LogInformation("Database migrations applied successfully");
            }
            else
            {
                _logger.LogInformation("No pending migrations found");
            }

            await SeedMasterAsync(masterDbContext, cancellationToken);

            // Применяем миграции для всех тенантов
            var initializer = scope.ServiceProvider.GetRequiredService<ITenantDatabaseInitializer>();
            var tenants = await masterDbContext.Tenants
                .Select(t => new { t.Id, t.ConnectionString, t.Subdomain })
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Migrating {Count} tenant database(s).", tenants.Count);

            foreach (var tenant in tenants)
            {
                try
                {
                    await initializer.MigrateOnlyAsync(tenant.ConnectionString, tenant.Subdomain, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to migrate tenant {TenantId}.", tenant.Id);
                }
            }

            _logger.LogInformation("All tenant migrations completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply database migrations");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedMasterAsync(MasterDbContext db, CancellationToken ct)
    {
        const string adminRoleName = "Admin";
        const string userRoleName = "User";
        const string adminEmail = "admin@gmail.com";
        const string adminPassword = "Admin1!";

        // Роль Admin
        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == adminRoleName, ct);
        if (adminRole is null)
        {
            adminRole = new MasterRole { Id = Guid.NewGuid(), Name = adminRoleName };
            db.Roles.Add(adminRole);
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Master: role '{Role}' created", adminRoleName);
        }

        // Роль User
        if (!await db.Roles.AnyAsync(r => r.Name == userRoleName, ct))
        {
            db.Roles.Add(new MasterRole { Id = Guid.NewGuid(), Name = userRoleName });
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Master: role '{Role}' created", userRoleName);
        }

        // Пользователь Admin
        var normalizedEmail = adminEmail.Trim().ToLowerInvariant();
        if (!await db.Users.AnyAsync(u => u.Email == normalizedEmail, ct))
        {
            var user = new MasterUser
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                HashPassword = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                FirstName = "Admin",
                LastName = "User",
                IsActive = true,
                IsEmailVerified = true
            };
            db.Users.Add(user);
            db.UserRoles.Add(new MasterUserRole { UserId = user.Id, RoleId = adminRole.Id });
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Master: default admin user created ({Email})", normalizedEmail);
        }
    }
}
