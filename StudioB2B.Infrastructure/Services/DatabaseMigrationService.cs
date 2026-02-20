using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Infrastructure.MultiTenancy;
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

            // В Development создаём демо-тенанта если его нет
            if (_environment.IsDevelopment())
            {
                await EnsureDemoTenantAsync(scope.ServiceProvider, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply database migrations");
            throw;
        }
    }

    private async Task EnsureDemoTenantAsync(IServiceProvider services, CancellationToken ct)
    {
        var tenantService = services.GetRequiredService<ITenantService>();
        var defaultSubdomain = _configuration.GetValue<string>("MultiTenancy:DefaultSubdomain") ?? "demo";

        var existingTenant = await tenantService.GetBySubdomainAsync(defaultSubdomain, ct);
        if (existingTenant != null)
        {
            _logger.LogInformation("Demo tenant '{Subdomain}' already exists", defaultSubdomain);
            return;
        }

        _logger.LogInformation("Creating demo tenant '{Subdomain}'...", defaultSubdomain);

        var result = await tenantService.RegisterAsync(
            companyName: "Demo Company",
            subdomain: defaultSubdomain,
            adminEmail: "admin@demo.local",
            adminPassword: "Demo123!",
            ct);

        if (result.Success)
        {
            _logger.LogInformation("Demo tenant created successfully. Login: admin@demo.local / Demo123!");
        }
        else
        {
            _logger.LogWarning("Failed to create demo tenant: {Error}", result.Error);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
