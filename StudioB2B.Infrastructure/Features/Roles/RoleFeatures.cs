using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities.Tenants;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Features.Roles;

public static class RoleQueryExtensions
{
    public static IQueryable<Role> OrderByName(this IQueryable<Role> query)
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

public class CreateRole(MasterDbContext db, IMapper mapper)
{
    public async Task<RoleDto> HandleAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        var role = mapper.Map<Role>(request);
        role.Id = Guid.NewGuid();

        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);

        return mapper.Map<RoleDto>(role);
    }
}

// ─── UpdateRole ───────────────────────────────────────────────────────────────

public class UpdateRole(MasterDbContext db, IMapper mapper)
{
    public async Task<RoleDto?> HandleAsync(Guid id, UpdateRoleRequest request, CancellationToken ct = default)
    {
        var role = await db.Roles.FindAsync([id], ct);
        if (role is null) return null;

        mapper.Map(request, role);

        await db.SaveChangesAsync(ct);

        return mapper.Map<RoleDto>(role);
    }
}

// ─── DeleteRole ───────────────────────────────────────────────────────────────

public class DeleteRole(MasterDbContext db)
{
    public async Task<bool> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var role = await db.Roles.FindAsync([id], ct);
        if (role is null) return false;

        role.IsDeleted = true;
        await db.SaveChangesAsync(ct);

        return true;
    }
}
