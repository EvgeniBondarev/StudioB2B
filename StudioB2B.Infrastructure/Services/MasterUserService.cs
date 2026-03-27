using StudioB2B.Infrastructure.Features;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Services;

/// <summary>
/// Сервис для управления мастер-пользователями и их ролями.
/// Инкапсулирует работу с MasterDbContext, используя extension-методы из MasterUserFeatures.
/// </summary>
public class MasterUserService : IMasterUserService
{
    private readonly MasterDbContext _db;

    public MasterUserService(MasterDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc/>
    public Task<MasterUserInitData> GetInitDataAsync(CancellationToken ct = default)
        => _db.GetMasterUserInitDataAsync(ct);

    /// <inheritdoc/>
    public Task UpdateUserRolesAsync(
        Guid userId,
        IEnumerable<Guid> toAdd,
        IEnumerable<Guid> toRemove,
        CancellationToken ct = default)
        => _db.UpdateUserRolesAsync(userId, toAdd, toRemove, ct);

    /// <inheritdoc/>
    public Task SetUserActiveAsync(Guid userId, bool isActive, CancellationToken ct = default)
        => _db.SetMasterUserActiveAsync(userId, isActive, ct);
}

