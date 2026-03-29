using StudioB2B.Infrastructure.Features;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Services;

/// <summary>
/// Сервис прав доступа: инкапсулирует все запросы к БД,
/// использует extension-методы из PermissionFeatures.
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly ITenantDbContextFactory _dbContextFactory;

    public PermissionService(ITenantDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc/>
    public async Task<List<PermissionDto>> GetAllAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetPermissionsAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<List<LabelValueDto>> GetAvailableAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetAvailablePermissionsAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<PermissionDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetPermissionByIdAsync(id, ct);
    }

    /// <inheritdoc/>
    public async Task<List<PageWithDetailsDto>> GetPagesWithDetailsAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetPagesWithDetailsAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, List<EntityOptionDto>>> GetEntityOptionsAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetEntityOptionsForPermissionAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error, Guid Id)> CreateAsync(
        CreatePermissionDto dto, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.CreatePermissionAsync(dto, ct);
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error)> UpdateAsync(
        Guid id, UpdatePermissionDto dto, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.UpdatePermissionAsync(id, dto, ct);
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error)> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.DeletePermissionAsync(id, ct);
    }
}

