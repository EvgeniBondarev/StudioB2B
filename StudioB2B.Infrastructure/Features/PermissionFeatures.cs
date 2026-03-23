using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Constants;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Features;

// ─────────────────────────────────────────────────────────────────────────────
// Queries
// ─────────────────────────────────────────────────────────────────────────────

public class GetPermissions(ITenantDbContextFactory factory)
{
    public async Task<List<PermissionDto>> HandleAsync(CancellationToken ct = default)
    {
        using var db = factory.CreateDbContext();
        var list = await QueryHelper.LoadWithRelations(db)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
        return list.Select(PermissionMapper.ToDto).ToList();
    }
}

public class GetPermissionById(ITenantDbContextFactory factory)
{
    public async Task<PermissionDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        using var db = factory.CreateDbContext();
        var p = await QueryHelper.LoadWithRelations(db).FirstOrDefaultAsync(x => x.Id == id, ct);
        return p is null ? null : PermissionMapper.ToDto(p);
    }
}

public class GetAvailablePermissions(ITenantDbContextFactory factory)
{
    public async Task<List<LabelValueDto>> HandleAsync(CancellationToken ct = default)
    {
        using var db = factory.CreateDbContext();
        return await db.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new LabelValueDto(p.Name, p.Id.ToString()))
            .ToListAsync(ct);
    }
}

/// <summary>Returns all Pages with their Columns and Functions for use in the edit dialog.</summary>
public class GetPagesWithDetails(ITenantDbContextFactory factory)
{
    public async Task<List<PageWithDetailsDto>> HandleAsync(CancellationToken ct = default)
    {
        using var db = factory.CreateDbContext();
        var pages = await db.Pages
            .AsNoTracking()
            .Include(p => p.Columns)
            .Include(p => p.Functions)
            .OrderBy(p => p.Id)
            .ToListAsync(ct);

        return pages.Select(p => new PageWithDetailsDto(
            p.Id,
            p.Name,
            p.DisplayName,
            p.Columns.Select(c => new LabelValueDto(string.IsNullOrEmpty(c.DisplayName) ? c.Name : c.DisplayName, c.Name)).ToList(),
            p.Functions.Select(f => new LabelValueDto(string.IsNullOrEmpty(f.DisplayName) ? f.Name : f.DisplayName, f.Name)).ToList()
        )).ToList();
    }
}

public record PageWithDetailsDto(int Id, string Name, string DisplayName, List<LabelValueDto> Columns, List<LabelValueDto> Functions);

public record EntityOptionDto(Guid Id, string Name);

/// <summary>Loads all entity options for each BlockedEntityType for use in the Permission edit dialog.</summary>
public class GetEntityOptionsForPermission(ITenantDbContextFactory factory)
{
    public async Task<Dictionary<string, List<EntityOptionDto>>> HandleAsync(CancellationToken ct = default)
    {
        using var db = factory.CreateDbContext();
        var result = new Dictionary<string, List<EntityOptionDto>>();

        result[BlockedEntityTypeEnum.Warehouse.ToString()] = await db.Warehouses
            .AsNoTracking().OrderBy(w => w.Name)
            .Select(w => new EntityOptionDto(w.Id, w.Name)).ToListAsync(ct);

        result[BlockedEntityTypeEnum.OrderStatus.ToString()] = await db.OrderStatuses
            .AsNoTracking().Where(s => s.IsInternal).OrderBy(s => s.Name)
            .Select(s => new EntityOptionDto(s.Id, s.Name)).ToListAsync(ct);

        result[BlockedEntityTypeEnum.MarketplaceClient.ToString()] =
            db.MarketplaceClients == null ? [] :
            await db.MarketplaceClients.AsNoTracking().OrderBy(c => c.Name)
                .Select(c => new EntityOptionDto(c.Id, c.Name)).ToListAsync(ct);

        result[BlockedEntityTypeEnum.Supplier.ToString()] = await db.Suppliers
            .AsNoTracking().OrderBy(s => s.Name)
            .Select(s => new EntityOptionDto(s.Id, s.Name)).ToListAsync(ct);

        result[BlockedEntityTypeEnum.DeliveryMethod.ToString()] = await db.DeliveryMethods
            .AsNoTracking().OrderBy(d => d.Name)
            .Select(d => new EntityOptionDto(d.Id, d.Name)).ToListAsync(ct);

        return result;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Commands
// ─────────────────────────────────────────────────────────────────────────────

public class CreatePermission(ITenantDbContextFactory factory)
{
    public async Task<(bool Success, string? Error, Guid Id)> HandleAsync(
        CreatePermissionDto request, CancellationToken ct = default)
    {
        using var db = factory.CreateDbContext();

        if (await db.Permissions.AnyAsync(p => p.Name == request.Name && !p.IsDeleted, ct))
            return (false, "Право с таким названием уже существует", Guid.Empty);

        var perm = new Permission
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            IsFullAccess = request.IsFullAccess
        };
        db.Permissions.Add(perm);
        await db.SaveChangesAsync(ct);

        await PermissionMapper.ApplyRelationsAsync(db, perm, request.Pages, request.PageColumns, request.Functions, request.BlockedEntities, ct);
        await db.SaveChangesAsync(ct);
        return (true, null, perm.Id);
    }
}

public class UpdatePermission(ITenantDbContextFactory factory)
{
    public async Task<(bool Success, string? Error)> HandleAsync(
        Guid id, UpdatePermissionDto request, CancellationToken ct = default)
    {
        using var db = factory.CreateDbContext();

        var perm = await db.Permissions
            .Include(p => p.Pages)
            .Include(p => p.PageColumns)
            .Include(p => p.Functions)
            .Include(p => p.BlockedEntities)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (perm is null) return (false, "Право не найдено");

        if (await db.Permissions.AnyAsync(p => p.Name == request.Name && p.Id != id && !p.IsDeleted, ct))
            return (false, "Право с таким названием уже существует");

        perm.Name = request.Name.Trim();
        perm.IsFullAccess = request.IsFullAccess;

        db.Set<PermissionPage>().RemoveRange(perm.Pages);
        db.Set<PermissionPageColumn>().RemoveRange(perm.PageColumns);
        db.Set<PermissionFunction>().RemoveRange(perm.Functions);
        db.Set<BlockedEntity>().RemoveRange(perm.BlockedEntities);
        await db.SaveChangesAsync(ct);

        await PermissionMapper.ApplyRelationsAsync(db, perm, request.Pages, request.PageColumns, request.Functions, request.BlockedEntities, ct);
        await db.SaveChangesAsync(ct);
        return (true, null);
    }
}

public class DeletePermission(ITenantDbContextFactory factory)
{
    public async Task<(bool Success, string? Error)> HandleAsync(Guid id, CancellationToken ct = default)
    {
        using var db = factory.CreateDbContext();
        var perm = await db.Permissions.FindAsync([id], ct);
        if (perm is null) return (false, "Право не найдено");
        perm.IsDeleted = true;
        await db.SaveChangesAsync(ct);
        return (true, null);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Internal helpers (shared within the assembly)
// ─────────────────────────────────────────────────────────────────────────────

internal static class PermissionMapper
{
    internal static PermissionDto ToDto(Permission p) => new(
        p.Id,
        p.Name,
        p.IsFullAccess,
        p.Pages.Select(pp => pp.Page.Name).ToList(),
        p.PageColumns.Select(pc => pc.PageColumn.Name).ToList(),
        p.Functions.Select(pf => pf.Function.Name).ToList(),
        p.BlockedEntities.Select(be => new BlockedEntityDto(be.Id, be.EntityType.ToString(), be.EntityId)).ToList());

    internal static async Task ApplyRelationsAsync(
        TenantDbContext db, Permission perm,
        List<string> pages, List<string> pageColumns, List<string> functions,
        List<SaveBlockedEntityDto> blockedEntities, CancellationToken ct)
    {
        foreach (var name in pages)
        {
            var page = await db.Pages.FirstOrDefaultAsync(p => p.Name == name, ct);
            if (page is not null)
                db.Set<PermissionPage>().Add(new PermissionPage { PermissionId = perm.Id, PageId = page.Id });
        }
        foreach (var name in pageColumns)
        {
            var col = await db.PageColumns.FirstOrDefaultAsync(c => c.Name == name, ct);
            if (col is not null)
                db.Set<PermissionPageColumn>().Add(new PermissionPageColumn { PermissionId = perm.Id, PageColumnId = col.Id });
        }
        foreach (var name in functions)
        {
            var func = await db.Functions.FirstOrDefaultAsync(f => f.Name == name, ct);
            if (func is not null)
                db.Set<PermissionFunction>().Add(new PermissionFunction { PermissionId = perm.Id, FunctionId = func.Id });
        }
        foreach (var be in blockedEntities)
        {
            if (Enum.TryParse<BlockedEntityTypeEnum>(be.EntityType, out var entityType))
                db.BlockedEntities.Add(new BlockedEntity
                {
                    Id = Guid.NewGuid(),
                    PermissionId = perm.Id,
                    EntityType = entityType,
                    EntityId = be.EntityId
                });
        }
    }
}

// shared for GetPermissions / GetPermissionById / UpdatePermission
file static class QueryHelper
{
    internal static IQueryable<Permission> LoadWithRelations(TenantDbContext db) =>
        db.Permissions
            .AsNoTracking()
            .Include(p => p.Pages).ThenInclude(pp => pp.Page)
            .Include(p => p.PageColumns).ThenInclude(pc => pc.PageColumn)
            .Include(p => p.Functions).ThenInclude(pf => pf.Function)
            .Include(p => p.BlockedEntities);
}

