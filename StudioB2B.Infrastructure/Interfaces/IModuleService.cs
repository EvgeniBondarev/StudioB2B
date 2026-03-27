using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Interfaces;

public interface IModuleService
{
    Task<bool> IsEnabledAsync(string moduleCode, CancellationToken ct = default);
    Task<List<TenantModule>> GetAllAsync(CancellationToken ct = default);
    Task EnableAsync(string moduleCode, CancellationToken ct = default);
    Task DisableAsync(string moduleCode, CancellationToken ct = default);

    /// <summary>
    /// Количество производителей (для карточки модуля на странице).
    /// </summary>
    Task<int> GetManufacturerCountAsync(CancellationToken ct = default);

    /// <summary>
    /// Гарантирует наличие всех базовых записей в TenantModules.
    /// </summary>
    Task EnsureModulesSeededAsync(CancellationToken ct = default);
}
