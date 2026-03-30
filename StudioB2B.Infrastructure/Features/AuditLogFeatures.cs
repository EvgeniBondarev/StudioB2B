using System.Linq.Dynamic.Core;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Features;

public static class AuditLogExtensions
{

    public static async Task<List<string>> GetAuditEntityNamesAsync(
        this TenantDbContext db, CancellationToken ct = default) =>
        await db.FieldAuditLogs
            .Select(x => x.EntityName)
            .Distinct()
            .OrderBy(x => x)
            .AsNoTracking()
            .ToListAsync(ct);

    public static async Task<List<string>> GetAuditUserNamesAsync(
        this TenantDbContext db, CancellationToken ct = default) =>
        await db.FieldAuditLogs
            .Where(x => x.ChangedByUserName != null)
            .Select(x => x.ChangedByUserName!)
            .Distinct()
            .OrderBy(x => x)
            .AsNoTracking()
            .ToListAsync(ct);

    private static IQueryable<FieldAuditLog> ApplyFilter(
        this IQueryable<FieldAuditLog> q, AuditLogFilter filter)
    {
        if (!string.IsNullOrEmpty(filter.EntityName))
            q = q.Where(x => x.EntityName == filter.EntityName);

        if (!string.IsNullOrEmpty(filter.ChangeType))
            q = q.Where(x => x.ChangeType == filter.ChangeType);

        if (!string.IsNullOrEmpty(filter.UserName))
            q = q.Where(x => x.ChangedByUserName == filter.UserName);

        if (filter.FromUtc.HasValue)
            q = q.Where(x => x.ChangedAtUtc >= filter.FromUtc.Value);

        if (filter.ToUtc.HasValue)
            q = q.Where(x => x.ChangedAtUtc <= filter.ToUtc.Value);

        return q;
    }

    public static async Task<(List<FieldAuditLog> Items, int Total)> GetAuditPagedAsync(
        this TenantDbContext db,
        AuditLogFilter filter,
        int skip,
        int take,
        string? orderBy,
        CancellationToken ct = default)
    {
        var q = db.FieldAuditLogs.AsQueryable().ApplyFilter(filter);

        var total = await q.CountAsync(ct);

        q = !string.IsNullOrEmpty(orderBy)
            ? q.OrderBy(orderBy)
            : q.OrderByDescending(x => x.ChangedAtUtc);

        var items = await q.Skip(skip).Take(take).AsNoTracking().ToListAsync(ct);
        return (items, total);
    }

    public static async Task<List<FieldAuditLog>> GetAuditByEntityAsync(
        this TenantDbContext db,
        string entityName,
        string entityId,
        CancellationToken ct = default) =>
        await db.FieldAuditLogs
            .Where(x => x.EntityName == entityName && x.EntityId == entityId)
            .OrderByDescending(x => x.ChangedAtUtc)
            .AsNoTracking()
            .ToListAsync(ct);

    public static async Task<List<FieldAuditLog>> GetAuditBySubjectsAsync(
        this TenantDbContext db,
        IReadOnlyList<AuditSubject> subjects,
        CancellationToken ct = default)
    {
        var entityNames = subjects.Select(s => s.EntityName).Distinct().ToList();
        var entityIds = subjects.Select(s => s.EntityId).Distinct().ToList();

        return await db.FieldAuditLogs
            .Where(x => entityNames.Contains(x.EntityName) && entityIds.Contains(x.EntityId))
            .OrderByDescending(x => x.ChangedAtUtc)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public static async Task<IReadOnlyDictionary<string, string>> BuildAuditValueResolverAsync(
        this TenantDbContext db,
        List<FieldAuditLog> logs,
        CancellationToken ct = default)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var statusIds = ExtractGuids(logs, "StatusId", "SystemStatusId");
        if (statusIds.Count > 0)
        {
            var items = await db.OrderStatuses.IgnoreQueryFilters()
                .Where(x => statusIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Name })
                .ToListAsync(ct);
            foreach (var item in items) d[item.Id.ToString()] = item.Name;
        }

        var methodIds = ExtractGuids(logs, "DeliveryMethodId");
        if (methodIds.Count > 0)
        {
            var items = await db.DeliveryMethods.IgnoreQueryFilters()
                .Where(x => methodIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Name })
                .ToListAsync(ct);
            foreach (var item in items) d[item.Id.ToString()] = item.Name;
        }

        var clientIds = ExtractGuids(logs, "MarketplaceClientId");
        if (clientIds.Count > 0 && db.MarketplaceClients != null)
        {
            var items = await db.MarketplaceClients.IgnoreQueryFilters()
                .Where(x => clientIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Name })
                .ToListAsync(ct);
            foreach (var item in items) d[item.Id.ToString()] = item.Name;
        }

        var categoryIds = ExtractGuids(logs, "CategoryId");
        if (categoryIds.Count > 0)
        {
            var items = await db.Categories.IgnoreQueryFilters()
                .Where(x => categoryIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Name })
                .ToListAsync(ct);
            foreach (var item in items) d[item.Id.ToString()] = item.Name;
        }

        var mfgIds = ExtractGuids(logs, "ManufacturerId");
        if (mfgIds.Count > 0)
        {
            var items = await db.Manufacturers.IgnoreQueryFilters()
                .Where(x => mfgIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Name })
                .ToListAsync(ct);
            foreach (var item in items) d[item.Id.ToString()] = item.Name;
        }

        var ptIds = ExtractGuids(logs, "PriceTypeId");
        if (ptIds.Count > 0)
        {
            var items = await db.PriceTypes.IgnoreQueryFilters()
                .Where(x => ptIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Name })
                .ToListAsync(ct);
            foreach (var item in items) d[item.Id.ToString()] = item.Name;
        }

        var currencyIds = ExtractGuids(logs, "CurrencyId");
        if (currencyIds.Count > 0)
        {
            var items = await db.Currencies.IgnoreQueryFilters()
                .Where(x => currencyIds.Contains(x.Id))
                .Select(x => new { x.Id, Display = x.Name ?? x.Code })
                .ToListAsync(ct);
            foreach (var item in items) d[item.Id.ToString()] = item.Display;
        }

        var warehouseIds = ExtractGuids(logs, "WarehouseId", "SenderWarehouseId");
        if (warehouseIds.Count > 0)
        {
            var items = await db.Warehouses.IgnoreQueryFilters()
                .Where(x => warehouseIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Name })
                .ToListAsync(ct);
            foreach (var item in items) d[item.Id.ToString()] = item.Name;
        }

        return d;
    }

    private static HashSet<Guid> ExtractGuids(List<FieldAuditLog> logs, params string[] fieldNames)
    {
        var set = new HashSet<Guid>();
        foreach (var log in logs)
        {
            if (!fieldNames.Contains(log.FieldName)) continue;
            if (TryParseGuidJson(log.OldValue, out var g1)) set.Add(g1);
            if (TryParseGuidJson(log.NewValue, out var g2)) set.Add(g2);
        }
        return set;
    }

    private static bool TryParseGuidJson(string? json, out Guid guid)
    {
        guid = Guid.Empty;
        if (json is null) return false;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var el = doc.RootElement;
            if (el.ValueKind == JsonValueKind.String)
                return Guid.TryParse(el.GetString(), out guid);
        }
        catch { }
        return false;
    }
}

