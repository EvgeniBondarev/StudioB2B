using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Domain.Entities.Tenants;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Shared.DTOs;

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


// ─── GetRoles ─────────────────────────────────────────────────────────────────

public class GetRoles(MasterDbContext db, IMapper mapper)
{
    public async Task<List<RoleDto>> HandleAsync(CancellationToken ct = default)
    {
        return await db.Roles
            .AsNoTracking()
            .OrderByName()
            .ProjectTo<RoleDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);
    }
}

// ─── GetRoleById ──────────────────────────────────────────────────────────────

public class GetRoleById(MasterDbContext db, IMapper mapper)
{
    public async Task<RoleDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        return await db.Roles
            .AsNoTracking()
            .Where(r => r.Id == id)
            .ProjectTo<RoleDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(ct);
    }
}

// ─── CreateRole ───────────────────────────────────────────────────────────────

public class CreateRole(MasterDbContext db, IMapper mapper, IRoleSyncPublisher sync)
{
    public async Task<RoleDto> HandleAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        var role = mapper.Map<MasterRole>(request);

        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);

        sync.Publish(role.Id, role.Name, role.Description, role.IsSystemRole, RoleSyncAction.Upsert);

        return mapper.Map<RoleDto>(role);
    }
}

// ─── UpdateRole ───────────────────────────────────────────────────────────────

public class UpdateRole(MasterDbContext db, IMapper mapper, IRoleSyncPublisher sync)
{
    public async Task<RoleDto?> HandleAsync(Guid id, UpdateRoleRequest request, CancellationToken ct = default)
    {
        var role = await db.Roles.FindAsync([id], ct);
        if (role is null) return null;

        mapper.Map(request, role);

        await db.SaveChangesAsync(ct);

        sync.Publish(role.Id, role.Name, role.Description, role.IsSystemRole, RoleSyncAction.Upsert);

        return mapper.Map<RoleDto>(role);
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

