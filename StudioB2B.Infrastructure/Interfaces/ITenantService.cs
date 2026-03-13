using StudioB2B.Domain.Entities;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Сервис для управления тенантами
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Получить тенанта по субдомену
    /// </summary>
    Task<TenantEntity?> GetBySubdomainAsync(string subdomain, CancellationToken ct = default);

    /// <summary>
    /// Получить тенанта по ID
    /// </summary>
    Task<TenantEntity?> GetByIdAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Проверить доступность субдомена
    /// </summary>
    Task<bool> IsSubdomainAvailableAsync(string subdomain, CancellationToken ct = default);

    /// <summary>
    /// Зарегистрировать нового тенанта и создать его базу данных
    /// </summary>
    Task<TenantRegistrationResultDto> RegisterAsync(string companyName, string subdomain, string adminEmail, string adminPassword,
                                                 CancellationToken ct = default);
}
