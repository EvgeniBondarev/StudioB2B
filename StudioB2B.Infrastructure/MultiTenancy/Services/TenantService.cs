using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.MultiTenancy.Initialization;
using StudioB2B.Infrastructure.Persistence.Master;

namespace StudioB2B.Infrastructure.MultiTenancy.Services;


public partial class TenantService : ITenantService
{
    private readonly MasterDbContext _masterDb;
    private readonly ITenantDatabaseInitializer _dbInitializer;
    private readonly TenantHangfireManager _hangfireManager;
    private readonly ILogger<TenantService> _logger;
    private readonly MultiTenancyOptions _options;

    public TenantService(
        MasterDbContext masterDb,
        ITenantDatabaseInitializer dbInitializer,
        TenantHangfireManager hangfireManager,
        ILogger<TenantService> logger,
        IOptions<MultiTenancyOptions> options)
    {
        _masterDb = masterDb;
        _dbInitializer = dbInitializer;
        _hangfireManager = hangfireManager;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<TenantEntity?> GetBySubdomainAsync(string subdomain, CancellationToken ct = default) =>
        await _masterDb.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain, ct);

    public async Task<TenantEntity?> GetByIdAsync(Guid tenantId, CancellationToken ct = default) =>
        await _masterDb.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

    public async Task<bool> IsSubdomainAvailableAsync(string subdomain, CancellationToken ct = default)
    {
        var normalized = subdomain.ToLowerInvariant().Trim();

        if (_options.ReservedSubdomains.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            return false;

        return !await _masterDb.Tenants.AnyAsync(t => t.Subdomain == normalized, ct);
    }

    public async Task<TenantRegistrationResult> RegisterAsync(
        string companyName, string subdomain, string adminEmail, string adminPassword,
        CancellationToken ct = default)
    {
        try
        {
            var normalized = subdomain.ToLowerInvariant().Trim();

            if (!IsValidSubdomain(normalized))
                return Fail("Invalid subdomain format. Use only letters, numbers, and hyphens (3-30 chars).");

            if (!await IsSubdomainAvailableAsync(normalized, ct))
                return Fail("Subdomain is already taken or reserved.");

            var connectionString = BuildConnectionString(normalized);

            var tenant = new TenantEntity
            {
                Name = companyName,
                Subdomain = normalized,
                ConnectionString = connectionString
            };

            _masterDb.Tenants.Add(tenant);
            await _masterDb.SaveChangesAsync(ct);
            _logger.LogInformation("Tenant record created: {TenantId} ({Subdomain})", tenant.Id, normalized);

            try
            {
                await _dbInitializer.MigrateAndSeedAsync(connectionString, ct);
                await _dbInitializer.CreateAdminUserAsync(connectionString, adminEmail, adminPassword, ct);
                await _hangfireManager.AddTenant(tenant.Id, connectionString, ct);

                _logger.LogInformation("Tenant registration completed: {TenantId}", tenant.Id);
                return new TenantRegistrationResult(true, tenant.Id);
            }
            catch
            {
                await RollbackAsync(tenant, connectionString, normalized, ct);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register tenant: {Subdomain}", subdomain);
            return Fail("Registration failed. Please try again later.");
        }
    }

    private string BuildConnectionString(string subdomain) =>
        string.Format(_options.TenantDbConnectionTemplate, $"StudioB2B_Tenant_{subdomain}");

    private async Task RollbackAsync(
        TenantEntity tenantEntity, string connectionString, string subdomain, CancellationToken ct)
    {
        _logger.LogWarning("Rolling back registration for {Subdomain}", subdomain);

        try { await _dbInitializer.DropDatabaseAsync(connectionString, ct); }
        catch (Exception ex) { _logger.LogError(ex, "Failed to drop DB during rollback for {Subdomain}", subdomain); }

        try { _masterDb.Tenants.Remove(tenantEntity); await _masterDb.SaveChangesAsync(ct); }
        catch (Exception ex) { _logger.LogError(ex, "Failed to remove tenant record during rollback for {Subdomain}", subdomain); }
    }

    private static TenantRegistrationResult Fail(string error) =>
        new(false, Error: error);

    private static bool IsValidSubdomain(string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain)) return false;
        if (subdomain.Length is < 3 or > 30) return false;
        return SubdomainRegex().IsMatch(subdomain);
    }

    [GeneratedRegex(@"^[a-z0-9][a-z0-9\-]*[a-z0-9]$|^[a-z0-9]$")]
    private static partial Regex SubdomainRegex();
}
