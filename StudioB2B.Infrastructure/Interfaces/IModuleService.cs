using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Interfaces;

public interface IModuleService
{
    Task<bool> IsEnabledAsync(string moduleCode, CancellationToken ct = default);
    Task<List<TenantModule>> GetAllAsync(CancellationToken ct = default);
    Task EnableAsync(string moduleCode, CancellationToken ct = default);
    Task DisableAsync(string moduleCode, CancellationToken ct = default);
}
