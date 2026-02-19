using Microsoft.EntityFrameworkCore;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Domain.Entities.Tenants;
using StudioB2B.Infrastructure.Persistence.Master;

namespace StudioB2B.Infrastructure.Features.Roles;

/// <summary>
/// Расширения для построения запросов к ролям (IQueryable)
/// </summary>
public static class RoleQueryExtensions
{
    public static IQueryable<MasterRole> ByNormalizedName(this IQueryable<MasterRole> query, string name)
    {
        var normalized = name.ToUpperInvariant();
        return query.Where(r => r.NormalizedName == normalized);
    }

    public static IQueryable<MasterRole> OrderByName(this IQueryable<MasterRole> query)
        => query.OrderBy(r => r.Name);
}

// ─── DTOs ────────────────────────────────────────────────────────────────────

public record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemRole,
    DateTime CreatedAtUtc);

public record CreateRoleRequest(string Name, string? Description, bool IsSystemRole);
public record UpdateRoleRequest(string Name, string? Description, bool IsSystemRole);

// ─── GetRoles ─────────────────────────────────────────────────────────────────

public class GetRoles(MasterDbContext db)
{
    public async Task<List<RoleDto>> HandleAsync(CancellationToken ct = default)
    {
        return await db.Roles
            .AsNoTracking()
            .OrderByName()
            .Select(r => new RoleDto(r.Id, r.Name, r.Description, r.IsSystemRole, r.CreatedAtUtc))
            .ToListAsync(ct);
    }
}

// ─── GetRoleById ──────────────────────────────────────────────────────────────

public class GetRoleById(MasterDbContext db)
{
    public async Task<RoleDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        return await db.Roles
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new RoleDto(r.Id, r.Name, r.Description, r.IsSystemRole, r.CreatedAtUtc))
            .FirstOrDefaultAsync(ct);
    }
}

// ─── CreateRole ───────────────────────────────────────────────────────────────

public class CreateRole(MasterDbContext db, IRoleSyncPublisher sync)
{
    public async Task<RoleDto> HandleAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        var role = new MasterRole
        {
            Name = request.Name.Trim(),
            NormalizedName = request.Name.Trim().ToUpperInvariant(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            Description = request.Description?.Trim(),
            IsSystemRole = request.IsSystemRole
        };

        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);

        sync.Publish(role.Id, role.Name, role.Description, role.IsSystemRole, RoleSyncAction.Upsert);

        return new RoleDto(role.Id, role.Name, role.Description, role.IsSystemRole, role.CreatedAtUtc);
    }
}

// ─── UpdateRole ───────────────────────────────────────────────────────────────

public class UpdateRole(MasterDbContext db, IRoleSyncPublisher sync)
{
    public async Task<RoleDto?> HandleAsync(Guid id, UpdateRoleRequest request, CancellationToken ct = default)
    {
        var role = await db.Roles.FindAsync([id], ct);
        if (role is null) return null;

        role.Name = request.Name.Trim();
        role.NormalizedName = request.Name.Trim().ToUpperInvariant();
        role.Description = request.Description?.Trim();
        role.IsSystemRole = request.IsSystemRole;
        role.ConcurrencyStamp = Guid.NewGuid().ToString();

        await db.SaveChangesAsync(ct);

        sync.Publish(role.Id, role.Name, role.Description, role.IsSystemRole, RoleSyncAction.Upsert);

        return new RoleDto(role.Id, role.Name, role.Description, role.IsSystemRole, role.CreatedAtUtc);
    }
}

// ─── DeleteRole ───────────────────────────────────────────────────────────────

public class DeleteRole(MasterDbContext db, IRoleSyncPublisher sync)
{
    public async Task<bool> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var role = await db.Roles.FindAsync([id], ct);
        if (role is null) return false;

        db.Roles.Remove(role);
        await db.SaveChangesAsync(ct);

        sync.Publish(id, role.Name, role.Description, role.IsSystemRole, RoleSyncAction.Delete);

        return true;
    }
}

