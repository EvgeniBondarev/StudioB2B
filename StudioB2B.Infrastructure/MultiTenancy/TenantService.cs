using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Domain.Entities.Tenants;
using StudioB2B.Domain.Extensions;
using StudioB2B.Domain.Options;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Infrastructure.Persistence.Master;

namespace StudioB2B.Infrastructure.MultiTenancy;


public partial class TenantService : ITenantService
{
    private readonly MasterDbContext _masterDb;
    private readonly ITenantDatabaseInitializer _dbInitializer;
    private readonly MultiTenancyOptions _options;

    public TenantService(MasterDbContext masterDb, ITenantDatabaseInitializer dbInitializer, IOptions<MultiTenancyOptions> options)
    {
        _masterDb = masterDb;
        _dbInitializer = dbInitializer;
        _options = options.Value;
    }

    public async Task<TenantEntity?> GetBySubdomainAsync(string subdomain, CancellationToken ct = default) =>
        await _masterDb.Tenants.GetByPredicateAsync(t => t.Subdomain == subdomain, ct);

    public async Task<TenantEntity?> GetByIdAsync(Guid tenantId, CancellationToken ct = default) =>
        await _masterDb.Tenants.GetByPredicateAsync(t => t.Id == tenantId, ct);

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

            if (!normalized.IsValidSubdomain())
                return Fail("Invalid subdomain format. Use only letters, numbers, and hyphens (3-30 chars).");

            if (!await IsSubdomainAvailableAsync(normalized, ct))
                return Fail("Subdomain is already taken or reserved.");

            var connectionString = string.Format(_options.TenantDbConnectionTemplate, $"{normalized}");

            var tenant = new TenantEntity
            {
                Name = companyName,
                Subdomain = normalized,
                ConnectionString = connectionString
            };

            _masterDb.Tenants.Add(tenant);
            await _masterDb.SaveChangesAsync(ct);

            try
            {
                await _dbInitializer.MigrateAndSeedAsync(connectionString, adminEmail, adminPassword, ct);
                return new TenantRegistrationResult(true, tenant.Id);
            }
            catch
            {
                await RollbackAsync(tenant, ct);
                throw;
            }
        }
        catch
        {
            return Fail("Registration failed. Please try again later.");
        }
    }

    private async Task RollbackAsync(TenantEntity tenantEntity, CancellationToken ct)
    {
        await _dbInitializer.DropDatabaseAsync(tenantEntity.ConnectionString, ct);
        _masterDb.Tenants.Remove(tenantEntity);
        await _masterDb.SaveChangesAsync(ct);
    }

    private static TenantRegistrationResult Fail(string error) =>
        new(false, Error: error);
}
