using System.Collections.Concurrent;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.Services.MultiTenancy;

public class TenantDbContextFactory : ITenantDbContextFactory
{
    // Cache ServerVersion per connection string — avoids a synchronous DB ping on every CreateDbContext() call.
    private static readonly ConcurrentDictionary<string, ServerVersion> _serverVersionCache = new();

    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly ILogger<TenantDbContextFactory> _logger;

    public TenantDbContextFactory(
        ITenantProvider tenantProvider,
        ICurrentUserProvider currentUserProvider,
        ILogger<TenantDbContextFactory> logger)
    {
        _tenantProvider = tenantProvider;
        _currentUserProvider = currentUserProvider;
        _logger = logger;
    }

    public TenantDbContext CreateDbContext()
    {
        if (!_tenantProvider.IsResolved)
        {
            throw new InvalidOperationException("Tenant is not resolved. Cannot create TenantDbContext.");
        }

        var connectionString = _tenantProvider.ConnectionString!;

        var serverVersion = _serverVersionCache.GetOrAdd(connectionString, cs => ServerVersion.AutoDetect(cs));

        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseMySql(connectionString, serverVersion);

        _logger.LogDebug("Creating TenantDbContext for tenant {TenantId}", _tenantProvider.TenantId);

        var context = new TenantDbContext(optionsBuilder.Options, _currentUserProvider);

        // Always apply pending tenant migrations before the first query.
        // Also, guard against "migration marked as applied but column missing"
        // by checking for the expected column and repairing it.
        try
        {
            var pending = context.Database.GetPendingMigrations().ToList();
            if (pending.Count > 0)
            {
                _logger.LogInformation(
                    "Applying {Count} pending tenant migrations for {TenantId}: {Migrations}",
                    pending.Count,
                    _tenantProvider.TenantId,
                    string.Join(", ", pending));
            }

            context.Database.Migrate();

            _logger.LogInformation(
                "Tenant migrations executed for {TenantId} (pending={PendingCount}).",
                _tenantProvider.TenantId,
                pending.Count);

            EnsureMarketplaceClientModeId2Column(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to apply tenant migrations for {TenantId}",
                _tenantProvider.TenantId);
            throw;
        }

        return context;
    }

    private static void EnsureMarketplaceClientModeId2Column(TenantDbContext context)
    {
        // If a tenant schema got into a bad state (e.g. migration history updated, but schema change not applied),
        // the app should not crash on simple SELECTs.
        var columnExists = ColumnExists(
            context,
            tableName: "MarketplaceClients",
            columnName: "ModeId2");

        if (columnExists)
            return;

        // Repair: add missing column (+ index). FK is handled by EF migrations later.
        context.Database.ExecuteSqlRaw(
            "ALTER TABLE MarketplaceClients ADD COLUMN ModeId2 char(36) NULL;");

        context.Database.ExecuteSqlRaw(
            "CREATE INDEX IX_MarketplaceClients_ModeId2 ON MarketplaceClients (ModeId2);");
    }

    private static bool ColumnExists(TenantDbContext context, string tableName, string columnName)
    {
        var conn = context.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            @"SELECT COUNT(*)
              FROM INFORMATION_SCHEMA.COLUMNS
              WHERE TABLE_SCHEMA = DATABASE()
                AND TABLE_NAME = @tableName
                AND COLUMN_NAME = @columnName;";

        var p1 = cmd.CreateParameter();
        p1.ParameterName = "@tableName";
        p1.Value = tableName;
        cmd.Parameters.Add(p1);

        var p2 = cmd.CreateParameter();
        p2.ParameterName = "@columnName";
        p2.Value = columnName;
        cmd.Parameters.Add(p2);

        var result = cmd.ExecuteScalar();
        return Convert.ToInt32(result) > 0;
    }
}
