using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.Interfaces;

public interface IModuleActivator
{
    string ModuleCode { get; }
    Task OnEnableAsync(TenantDbContext db, CancellationToken ct);
}
