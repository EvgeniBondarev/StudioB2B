using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.Features;

public static class PriceTypeExtensions
{
    /// <summary>
    /// Постраничная выборка типов цен с Dynamic LINQ-фильтром и сортировкой.
    /// </summary>
    public static async Task<(List<PriceType> Items, int TotalCount)> GetPriceTypesPagedAsync(
        this TenantDbContext db,
        string?           dynamicFilter,
        string?           orderBy,
        int               skip,
        int               take,
        CancellationToken ct = default)
    {
        var query = db.PriceTypes
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrEmpty(dynamicFilter))
            query = query.Where(dynamicFilter);

        var totalCount = await query.CountAsync(ct);

        query = !string.IsNullOrEmpty(orderBy)
            ? query.OrderBy(orderBy)
            : query.OrderBy(p => p.Name);

        var items = await query.Skip(skip).Take(take).ToListAsync(ct);
        return (items, totalCount);
    }

    /// <summary>Создать новый пользовательский тип цены.</summary>
    public static async Task<PriceType> CreatePriceTypeAsync(
        this TenantDbContext db,
        PriceType         item,
        CancellationToken ct = default)
    {
        db.PriceTypes.Add(item);
        await db.SaveChangesAsync(ct);
        return item;
    }

    /// <summary>
    /// Обновить поля пользовательского типа цены (Name, DeliveryScheme).
    /// Возвращает <c>false</c>, если запись не найдена или является системной.
    /// </summary>
    public static async Task<bool> UpdatePriceTypeAsync(
        this TenantDbContext db,
        PriceType         updatedItem,
        CancellationToken ct = default)
    {
        var entity = await db.PriceTypes.FindAsync(new object[] { updatedItem.Id }, ct);
        if (entity == null || !entity.IsUserDefined) return false;

        entity.Name           = updatedItem.Name.Trim();
        entity.DeliveryScheme = string.IsNullOrWhiteSpace(updatedItem.DeliveryScheme)
            ? null
            : updatedItem.DeliveryScheme.Trim();

        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Мягкое удаление пользовательского типа цены (IsDeleted = true).
    /// Возвращает <c>false</c>, если запись не найдена или является системной.
    /// </summary>
    public static async Task<bool> SoftDeletePriceTypeAsync(
        this TenantDbContext db,
        Guid              id,
        CancellationToken ct = default)
    {
        var entity = await db.PriceTypes.FindAsync(new object[] { id }, ct);
        if (entity == null || !entity.IsUserDefined) return false;

        entity.IsDeleted = true;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

