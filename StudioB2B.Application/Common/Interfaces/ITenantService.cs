using StudioB2B.Domain.Entities.Tenants;

namespace StudioB2B.Application.Common.Interfaces;

/// <summary>
/// Сервис для управления тенантами
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Получить тенанта по субдомену
    /// </summary>
    Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken ct = default);

    /// <summary>
    /// Получить тенанта по ID
    /// </summary>
    Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Проверить доступность субдомена
    /// </summary>
    Task<bool> IsSubdomainAvailableAsync(string subdomain, CancellationToken ct = default);

    /// <summary>
    /// Зарегистрировать нового тенанта и создать его базу данных
    /// </summary>
    Task<TenantRegistrationResult> RegisterAsync(
        string companyName,
        string subdomain,
        string adminEmail,
        string adminPassword,
        CancellationToken ct = default);

    /// <summary>
    /// Активировать/деактивировать тенанта
    /// </summary>
    Task<bool> SetActiveStateAsync(Guid tenantId, bool isActive, CancellationToken ct = default);
}

/// <summary>
/// Результат регистрации тенанта
/// </summary>
public record TenantRegistrationResult(
    bool Success,
    Guid? TenantId = null,
    string? Error = null);
