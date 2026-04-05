using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Features;

public static class ReturnExtensions
{
    /// <summary>
    /// Загружает счётчики по типам возвратов и количество отмен, привязанных к заказу.
    /// </summary>
    public static async Task<ReturnsCountsData> GetReturnsCountsAsync(
        this TenantDbContext db,
        CancellationToken ct = default)
    {
        var raw = await db.OrderReturns.AsNoTracking()
            .Where(r => r.Type != null)
            .GroupBy(r => r.Type!)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var typeCounts = raw.ToDictionary(x => x.Type, x => x.Count);

        var cancellationsWithOrderCount = await db.OrderReturns.AsNoTracking()
            .Where(r => r.Type == "Cancellation" && r.OrderId != null)
            .Select(r => r.OzonReturnId)
            .Distinct()
            .CountAsync(ct);

        return new ReturnsCountsData(typeCounts, cancellationsWithOrderCount);
    }

    /// <summary>
    /// Постраничный запрос возвратов с применением всех фильтров.
    /// </summary>
    public static async Task<ReturnsPageResult> GetReturnsPageAsync(
        this TenantDbContext db,
        ReturnsPageRequest request,
        CancellationToken ct = default)
    {
        var query = db.OrderReturns.AsNoTracking().AsQueryable();

        // Полнотекстовый поиск
        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var term = request.SearchText.Trim();
            query = query.Where(r =>
                (r.PostingNumber != null && r.PostingNumber.Contains(term)) ||
                (r.OrderNumber   != null && r.OrderNumber.Contains(term))   ||
                (r.ProductName   != null && r.ProductName.Contains(term))   ||
                (r.OfferId       != null && r.OfferId.Contains(term)));
        }

        // Динамический фильтр RadzenDataGrid
        if (!string.IsNullOrEmpty(request.DynamicFilter))
            query = query.Where(request.DynamicFilter);

        // Быстрые фильтры
        if (!string.IsNullOrEmpty(request.FilterType))
            query = query.Where(r => r.Type == request.FilterType);

        if (!string.IsNullOrEmpty(request.FilterSchema))
            query = query.Where(r => r.Schema == request.FilterSchema);

        if (request.FilterLinkedToOrder)
            query = query.Where(r => r.Type == "Cancellation" && r.OrderId != null);

        // Общее количество (distinct по OzonReturnId)
        var totalCount = await query
            .Select(r => r.OzonReturnId)
            .Distinct()
            .CountAsync(ct);

        // Сортировка
        query = !string.IsNullOrEmpty(request.OrderBy)
            ? query.OrderBy(request.OrderBy)
            : query.OrderByDescending(r => r.ReturnDate ?? r.SyncedAt);

        // Страница + дедубликация
        var items = (await query
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(ct))
            .DistinctBy(r => r.OzonReturnId)
            .ToList();

        return new ReturnsPageResult(items, totalCount);
    }
}

