using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Features;

public static class MasterUserExtensions
{

    public static Task<List<MasterRole>> GetAllMasterRolesAsync(
        this MasterDbContext db, CancellationToken ct = default)
        => db.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(ct);

    public static Task<List<MasterUser>> GetAllMasterUsersAsync(
        this MasterDbContext db, CancellationToken ct = default)
        => db.Users.AsNoTracking()
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

    public static async Task<Dictionary<Guid, List<Guid>>> GetUserRoleIdsAsync(
        this MasterDbContext db,
        IEnumerable<Guid> userIds,
        CancellationToken ct = default)
    {
        var ids = userIds.ToList();
        var links = await db.UserRoles
            .AsNoTracking()
            .Where(ur => ids.Contains(ur.UserId))
            .ToListAsync(ct);

        return links
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleId).ToList());
    }

    /// <summary>
    /// Один вызов вместо трёх: роли + пользователи + связи пользователь↔роль.
    /// </summary>
    public static async Task<MasterUserInitData> GetMasterUserInitDataAsync(
        this MasterDbContext db, CancellationToken ct = default)
    {
        var roles   = await db.GetAllMasterRolesAsync(ct);
        var users   = await db.GetAllMasterUsersAsync(ct);
        var roleIds = await db.GetUserRoleIdsAsync(users.Select(u => u.Id), ct);
        return new MasterUserInitData(users, roles, roleIds);
    }

    /// <summary>
    /// Атомарно добавляет и удаляет роли пользователя.
    /// При ошибке откатывает все изменения через Detach.
    /// </summary>
    public static async Task UpdateUserRolesAsync(
        this MasterDbContext db,
        Guid userId,
        IEnumerable<Guid> toAdd,
        IEnumerable<Guid> toRemove,
        CancellationToken ct = default)
    {
        var addList    = toAdd.ToList();
        var removeList = toRemove.ToList();

        try
        {
            foreach (var roleId in addList)
                db.UserRoles.Add(new MasterUserRole { UserId = userId, RoleId = roleId });

            if (removeList.Count > 0)
            {
                var linksToRemove = await db.UserRoles
                    .Where(ur => ur.UserId == userId && removeList.Contains(ur.RoleId))
                    .ToListAsync(ct);
                db.UserRoles.RemoveRange(linksToRemove);
            }

            await db.SaveChangesAsync(ct);
        }
        catch
        {
            // Откатываем несохранённые изменения в контексте
            foreach (var entry in db.ChangeTracker.Entries().ToList())
                entry.State = EntityState.Detached;
            throw;
        }
    }

    /// <summary>
    /// Устанавливает IsActive для мастер-пользователя.
    /// </summary>
    public static async Task SetMasterUserActiveAsync(
        this MasterDbContext db,
        Guid userId,
        bool isActive,
        CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync(new object[] { userId }, ct);
        if (user is null) return;
        user.IsActive = isActive;
        await db.SaveChangesAsync(ct);
    }
}

