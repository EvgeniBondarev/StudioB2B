using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

    public DatabaseMigrationService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseMigrationService> logger,
        IHostEnvironment environment,
        IConfiguration configuration)
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply database migrations");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
