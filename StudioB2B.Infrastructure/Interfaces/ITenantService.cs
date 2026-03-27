using StudioB2B.Domain.Entities;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Сервис для управления тенантами
/// </summary>
public interface ITenantService
{
    Task<TenantEntity?> GetBySubdomainAsync(string subdomain, CancellationToken ct = default);
    Task<TenantEntity?> GetByIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<bool> IsSubdomainAvailableAsync(string subdomain, CancellationToken ct = default);

    /// <summary>Зарегистрировать нового тенанта и создать его базу данных</summary>
    Task<TenantRegistrationResultDto> RegisterAsync(
        string companyName, string subdomain,
        string adminEmail, string adminPassword,
        string firstName, string lastName, string? middleName,
        Guid? createdByUserId = null,
        CancellationToken ct = default);

    /// <summary>Получить все тенанты (для Admin)</summary>
    Task<List<TenantEntity>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Получить тенанты конкретного пользователя</summary>
    Task<List<TenantEntity>> GetByCreatorAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Активировать / деактивировать тенант</summary>
    Task<bool> SetActiveAsync(Guid tenantId, bool isActive, CancellationToken ct = default);

    /// <summary>Мягкое удаление тенанта</summary>
    Task<bool> DeleteAsync(Guid tenantId, CancellationToken ct = default);
}
