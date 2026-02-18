using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Entities.Tenants;

/// <summary>
/// Тенант (арендатор) - клиент SaaS системы
/// Хранится в Master Database
/// </summary>
public class Tenant : AuditableEntity
{
    public string Name { get; private set; } = null!;
    public string Subdomain { get; private set; } = null!;
    public string ConnectionString { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;
    public string? LogoUrl { get; private set; }
    public TenantTariff Tariff { get; private set; } = TenantTariff.Trial;
    public DateTime? TrialExpiresAtUtc { get; private set; }

    private Tenant() { } // EF Core

    public static Tenant Create(string name, string subdomain, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(subdomain);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return new Tenant
        {
            Name = name,
            Subdomain = subdomain.ToLowerInvariant(),
            ConnectionString = connectionString,
            TrialExpiresAtUtc = DateTime.UtcNow.AddDays(14)
        };
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void UpdateConnectionString(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ConnectionString = connectionString;
    }

    public void SetTariff(TenantTariff tariff)
    {
        Tariff = tariff;
        if (tariff != TenantTariff.Trial)
        {
            TrialExpiresAtUtc = null;
        }
    }

    public void SetLogo(string? logoUrl) => LogoUrl = logoUrl;

    public bool IsTrialExpired() => Tariff == TenantTariff.Trial
        && TrialExpiresAtUtc.HasValue
        && TrialExpiresAtUtc.Value < DateTime.UtcNow;
}


