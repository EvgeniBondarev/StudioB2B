using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.Services.Modules;

public class ModuleService : IModuleService
{
    private readonly TenantDbContext _db;
    private readonly IEnumerable<IModuleActivator> _activators;
    private readonly ILogger<ModuleService> _logger;

    public ModuleService(
        TenantDbContext db,
        IEnumerable<IModuleActivator> activators,
        ILogger<ModuleService> logger)
    {
        _db = db;
        _activators = activators;
        _logger = logger;
    }

    public async Task<bool> IsEnabledAsync(string moduleCode, CancellationToken ct = default)
        => await _db.TenantModules.AnyAsync(m => m.Code == moduleCode && m.IsEnabled, ct);

    public async Task<List<TenantModule>> GetAllAsync(CancellationToken ct = default)
        => await _db.TenantModules.ToListAsync(ct);

    public async Task EnableAsync(string moduleCode, CancellationToken ct = default)
    {
        var module = await _db.TenantModules.FirstOrDefaultAsync(m => m.Code == moduleCode, ct);
        if (module == null) { _logger.LogWarning("Module {Code} not found", moduleCode); return; }

        if (!module.IsEnabled)
        {
            module.IsEnabled = true;
            module.EnabledAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        var activator = _activators.FirstOrDefault(a => a.ModuleCode == moduleCode);
        if (activator != null)
        {
            _logger.LogInformation("Running activator for module {Code}...", moduleCode);
            await activator.OnEnableAsync(_db, ct);
            _logger.LogInformation("Activator for module {Code} completed", moduleCode);
        }
    }

    public async Task DisableAsync(string moduleCode, CancellationToken ct = default)
    {
        var module = await _db.TenantModules.FirstOrDefaultAsync(m => m.Code == moduleCode, ct);
        if (module == null) { _logger.LogWarning("Module {Code} not found", moduleCode); return; }

        module.IsEnabled = false;
        module.DisabledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Module {Code} disabled", moduleCode);
    }

    /// <inheritdoc/>
    public Task<int> GetManufacturerCountAsync(CancellationToken ct = default)
        => _db.GetManufacturerCountAsync(ct);

    /// <inheritdoc/>
    public Task EnsureModulesSeededAsync(CancellationToken ct = default)
        => _db.EnsureModulesSeededAsync(ct);
}
