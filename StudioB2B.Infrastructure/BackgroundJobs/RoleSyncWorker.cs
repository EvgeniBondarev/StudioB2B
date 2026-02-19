using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.BackgroundJobs;

/// <summary>
/// Фоновый воркер, читает задания из RoleSyncChannel и применяет изменения ролей
/// ко всем активным тенантам
/// </summary>
public class RoleSyncWorker(
    RoleSyncChannel channel,
    IServiceProvider services,
    ILogger<RoleSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RoleSyncWorker started");

        await foreach (var job in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessJobAsync(job, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to sync role {RoleId} ({RoleName})", job.RoleId, job.RoleName);
            }
        }

        logger.LogInformation("RoleSyncWorker stopped");
    }

    private async Task ProcessJobAsync(RoleSyncJob job, CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var masterDb = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

        var tenants = await masterDb.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive)
            .Select(t => new { t.Id, t.Subdomain, t.ConnectionString })
            .ToListAsync(ct);

        logger.LogInformation(
            "Syncing role {RoleName} (op={Op}) to {Count} tenants",
            job.RoleName, job.Operation, tenants.Count);

        foreach (var tenant in tenants)
        {
            try
            {
                await ApplyToTenantAsync(job, tenant.ConnectionString, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to sync role {RoleName} to tenant {Subdomain}",
                    job.RoleName, tenant.Subdomain);
            }
        }
    }

    private static async Task ApplyToTenantAsync(RoleSyncJob job, string connectionString, CancellationToken ct)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

        await using var tenantDb = new TenantDbContext(optionsBuilder.Options);

        var existing = await tenantDb.Roles
            .FirstOrDefaultAsync(r => r.Id == job.RoleId, ct);

        switch (job.Operation)
        {
            case RoleSyncOperation.Upsert:
                if (existing is null)
                {
                    tenantDb.Roles.Add(new ApplicationRole
                    {
                        Id = job.RoleId,
                        Name = job.RoleName,
                        NormalizedName = job.NormalizedRoleName,
                        ConcurrencyStamp = Guid.NewGuid().ToString(),
                        Description = job.Description,
                        IsSystemRole = job.IsSystemRole
                    });
                }
                else
                {
                    existing.Name = job.RoleName;
                    existing.NormalizedName = job.NormalizedRoleName;
                    existing.Description = job.Description;
                    existing.IsSystemRole = job.IsSystemRole;
                    existing.ConcurrencyStamp = Guid.NewGuid().ToString();
                }
                break;

            case RoleSyncOperation.Delete:
                if (existing is not null)
                    tenantDb.Roles.Remove(existing);
                break;
        }

        await tenantDb.SaveChangesAsync(ct);
    }
}

