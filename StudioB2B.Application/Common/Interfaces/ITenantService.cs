using StudioB2B.Domain.Entities.Tenants;

namespace StudioB2B.Application.Common.Interfaces;

/// <summary>
/// Сервис для управления тенантами
/// </summary>
public interface ITenantService
{
    Task<TenantEntity?> GetBySubdomainAsync(string subdomain, CancellationToken ct = default);

    Task<TenantEntity?> GetByIdAsync(Guid tenantId, CancellationToken ct = default);

    Task<bool> IsSubdomainAvailableAsync(string subdomain, CancellationToken ct = default);

    Task<TenantRegistrationResult> RegisterAsync(
        string companyName,
        string subdomain,
        string adminEmail,
        string adminPassword,
        CancellationToken ct = default);
}

/// <summary>
/// Результат регистрации тенанта
/// </summary>
public record TenantRegistrationResult(
    bool Success,
    Guid? TenantId = null,
    string? Error = null);
