using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudioB2B.Domain.Entities;
using StudioB2B.Domain.Options;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Services.MultiTenancy;


public partial class TenantService : ITenantService
{
    private readonly MasterDbContext _masterDb;
    private readonly ITenantDatabaseInitializer _dbInitializer;
    private readonly TenantHangfireManager _hangfireManager;
    private readonly ILogger<TenantService> _logger;
    private readonly MultiTenancyOptions _options;

    public TenantService(MasterDbContext masterDb, ITenantDatabaseInitializer dbInitializer, TenantHangfireManager hangfireManager,
                         ILogger<TenantService> logger, IOptions<MultiTenancyOptions> options)
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

    public async Task<TenantRegistrationResultDto> RegisterAsync(
        string companyName, string subdomain,
        string adminEmail, string adminPassword,
        string firstName, string lastName, string? middleName,
        Guid? createdByUserId = null,
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

            if (createdByUserId.HasValue)
            {
                var userExists = await _masterDb.Users.AnyAsync(u => u.Id == createdByUserId.Value, ct);
                if (!userExists)
                    createdByUserId = null;
            }

            var tenant = new TenantEntity
            {
                Name = companyName,
                Subdomain = normalized,
                ConnectionString = connectionString,
                CreatedByUserId = createdByUserId
            };

            _masterDb.Tenants.Add(tenant);
            await _masterDb.SaveChangesAsync(ct);
            _logger.LogInformation("Tenant record created: {TenantId} ({Subdomain})", tenant.Id, normalized);

            try
            {
                await _dbInitializer.MigrateAndSeedAsync(connectionString, ct);
                await _dbInitializer.CreateAdminUserAsync(connectionString, adminEmail, adminPassword,
                    firstName, lastName, middleName, ct);
                await _hangfireManager.AddTenant(tenant.Id, connectionString, ct);

                _logger.LogInformation("Tenant registration completed: {TenantId}", tenant.Id);
                return new TenantRegistrationResultDto(true, tenant.Id);
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

    public async Task<List<TenantEntity>> GetAllAsync(CancellationToken ct = default) =>
        await _masterDb.Tenants.AsNoTracking()
            .Include(t => t.CreatedBy)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

    public async Task<List<TenantEntity>> GetByCreatorAsync(Guid userId, CancellationToken ct = default) =>
        await _masterDb.Tenants.AsNoTracking()
            .Include(t => t.CreatedBy)
            .Where(t => t.CreatedByUserId == userId)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

    public async Task<bool> SetActiveAsync(Guid tenantId, bool isActive, CancellationToken ct = default)
    {
        var tenant = await _masterDb.Tenants.FindAsync([tenantId], ct);
        if (tenant is null) return false;

        tenant.IsActive = isActive;
        await _masterDb.SaveChangesAsync(ct);
        _logger.LogInformation("Tenant {TenantId} IsActive set to {IsActive}", tenantId, isActive);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _masterDb.Tenants.FindAsync([tenantId], ct);
        if (tenant is null) return false;

        tenant.IsDeleted = true;
        await _masterDb.SaveChangesAsync(ct);
        _logger.LogInformation("Tenant {TenantId} soft-deleted", tenantId);
        return true;
    }

    private string BuildConnectionString(string subdomain) => string.Format(_options.TenantDbConnectionTemplate, $"StudioB2B_Tenant_{subdomain}");

    private async Task RollbackAsync(
        TenantEntity tenantEntity, string connectionString, string subdomain, CancellationToken ct)
    {
        _logger.LogWarning("Rolling back registration for {Subdomain}", subdomain);

        try { await _dbInitializer.DropDatabaseAsync(connectionString, ct); }
        catch (Exception ex) { _logger.LogError(ex, "Failed to drop DB during rollback for {Subdomain}", subdomain); }

        try { _masterDb.Tenants.Remove(tenantEntity); await _masterDb.SaveChangesAsync(ct); }
        catch (Exception ex) { _logger.LogError(ex, "Failed to remove tenant record during rollback for {Subdomain}", subdomain); }
    }

    private static TenantRegistrationResultDto Fail(string error) => new(false, Error: error);

    private static bool IsValidSubdomain(string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain)) return false;
        if (subdomain.Length is < 3 or > 30) return false;
        return SubdomainRegex().IsMatch(subdomain);
    }

    [GeneratedRegex(@"^[a-z0-9][a-z0-9\-]*[a-z0-9]$|^[a-z0-9]$")]
    private static partial Regex SubdomainRegex();
}
