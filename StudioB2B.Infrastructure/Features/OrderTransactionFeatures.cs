using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Constants;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Features;

/// <summary>
/// Extension-методы TenantDbContext для управления документами заказов (CRUD).
/// </summary>
public static class OrderTransactionManagementExtensions
{

    private static IQueryable<OrderTransaction> IncludeForGrid(this IQueryable<OrderTransaction> q)
        => q
            .Include(t => t.FromSystemStatus)
            .Include(t => t.ToSystemStatus)
            .Include(t => t.Rules);

    private static IQueryable<OrderTransaction> IncludeForCanvas(this IQueryable<OrderTransaction> q)
        => q
            .Include(t => t.Rules).ThenInclude(r => r.PriceType)
            .Include(t => t.FieldRules);

    private static IQueryable<OrderTransaction> IncludeForEdit(this IQueryable<OrderTransaction> q)
        => q
            .Include(t => t.Rules).ThenInclude(r => r.PriceType)
            .Include(t => t.FieldRules);

    /// <summary>
    /// Постраничная выборка документов заказов с Dynamic LINQ фильтрацией и сортировкой.
    /// </summary>
    public static async Task<(List<OrderTransaction> Items, int TotalCount)> GetOrderTransactionsPagedAsync(
        this TenantDbContext db,
        string? dynamicFilter,
        string? orderBy,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var query = db.OrderTransactions
            .IncludeForGrid()
            .Where(t => !t.IsDeleted)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrEmpty(dynamicFilter))
            query = query.Where(dynamicFilter);

        var totalCount = await query.CountAsync(ct);

        query = !string.IsNullOrEmpty(orderBy)
            ? query.OrderBy(orderBy)
            : query.OrderBy(t => t.Name);

        var items = await query.Skip(skip).Take(take).ToListAsync(ct);
        return (items, totalCount);
    }

    /// <summary>
    /// Все активные (IsEnabled) документы с включёнными статусами — для матрицы переходов.
    /// </summary>
    public static async Task<List<OrderTransaction>> GetEnabledOrderTransactionsWithStatusesAsync(
        this TenantDbContext db,
        CancellationToken ct = default)
    {
        return await db.OrderTransactions
            .Include(t => t.FromSystemStatus)
            .Include(t => t.ToSystemStatus)
            .Where(t => !t.IsDeleted && t.IsEnabled)
            .OrderBy(t => t.FromSystemStatus!.Name)
            .ThenBy(t => t.ToSystemStatus!.Name)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    /// <summary>
    /// Все данные для холста: статусы + документы с правилами.
    /// </summary>
    public static async Task<(List<OrderStatus> Statuses, List<OrderTransaction> Transactions)> GetCanvasDataAsync(
        this TenantDbContext db,
        CancellationToken ct = default)
    {
        var statuses = await db.OrderStatuses
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.Name)
            .AsNoTracking()
            .ToListAsync(ct);

        var transactions = await db.OrderTransactions
            .IncludeForCanvas()
            .Where(t => !t.IsDeleted)
            .AsNoTracking()
            .ToListAsync(ct);

        return (statuses, transactions);
    }

    /// <summary>
    /// Все (не удалённые) документы с правилами для обновления холста.
    /// </summary>
    public static async Task<List<OrderTransaction>> GetAllOrderTransactionsForCanvasAsync(
        this TenantDbContext db,
        CancellationToken ct = default)
    {
        return await db.OrderTransactions
            .IncludeForCanvas()
            .Where(t => !t.IsDeleted)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    /// <summary>
    /// Все (не удалённые) статусы заказов — для холста.
    /// </summary>
    public static async Task<List<OrderStatus>> GetAllOrderStatusesForCanvasAsync(
        this TenantDbContext db,
        CancellationToken ct = default)
    {
        return await db.OrderStatuses
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.Name)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    /// <summary>
    /// Загружает документ по Id со всеми правилами — для диалога редактирования.
    /// </summary>
    public static async Task<OrderTransaction?> GetOrderTransactionForEditAsync(
        this TenantDbContext db,
        Guid id,
        CancellationToken ct = default)
    {
        return await db.OrderTransactions
            .IncludeForEdit()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    /// <summary>
    /// Загружает справочные данные для диалога создания/редактирования документа.
    /// </summary>
    public static async Task<TransactionEditReferenceData> GetTransactionEditReferenceDataAsync(
        this TenantDbContext db,
        CancellationToken ct = default)
    {
        var internalStatuses = await db.OrderStatuses
            .Where(s => s.IsInternal && !s.IsDeleted)
            .OrderBy(s => s.Name)
            .AsNoTracking()
            .ToListAsync(ct);

        var nonTerminalStatuses = internalStatuses.Where(s => !s.IsTerminal).ToList();

        var priceTypes = await db.PriceTypes
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Name)
            .AsNoTracking()
            .ToListAsync(ct);

        var products = await db.Products
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Article)
            .Take(500)
            .AsNoTracking()
            .ToListAsync(ct);

        return new TransactionEditReferenceData(internalStatuses, nonTerminalStatuses, priceTypes, products);
    }

    /// <summary>
    /// Постраничная выборка истории проведения документов.
    /// </summary>
    public static async Task<(List<OrderTransactionHistory> Items, int TotalCount)> GetOrderTransactionHistoryPagedAsync(
        this TenantDbContext db,
        string? dynamicFilter,
        string? orderBy,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var query = db.OrderTransactionHistories
            .Include(h => h.Order!).ThenInclude(o => o!.Shipment)
            .Include(h => h.Order!).ThenInclude(o => o!.ProductInfo).ThenInclude(pi => pi!.Product)
            .Include(h => h.OrderTransaction)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrEmpty(dynamicFilter))
            query = query.Where(dynamicFilter);

        var totalCount = await query.CountAsync(ct);

        query = !string.IsNullOrEmpty(orderBy)
            ? query.OrderBy(orderBy)
            : query.OrderByDescending(h => h.PerformedAtUtc);

        var items = await query.Skip(skip).Take(take).ToListAsync(ct);
        return (items, totalCount);
    }

    /// <summary>Создаёт новый документ заказа со всеми правилами.</summary>
    public static async Task<OrderTransaction> CreateOrderTransactionAsync(
        this TenantDbContext db,
        SaveOrderTransactionRequest request,
        CancellationToken ct = default)
    {
        var t = new OrderTransaction
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            FromSystemStatusId = request.FromSystemStatusId,
            ToSystemStatusId = request.ToSystemStatusId,
            IsEnabled = request.IsEnabled,
            Color = request.Color,
            Icon = string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon.Trim(),
            SortOrder = 0
        };
        db.OrderTransactions.Add(t);
        await db.SaveChangesAsync(ct);

        var sortOrder = 0;
        foreach (var r in request.Rules)
        {
            db.OrderTransactionRules.Add(new OrderTransactionRule
            {
                Id = Guid.NewGuid(),
                OrderTransactionId = t.Id,
                PriceTypeId = r.PriceTypeId,
                ValueSource = r.ValueSource,
                Formula = r.ValueSource == TransactionValueSourceEnum.Formula ? r.Formula : null,
                ProductId = r.ProductId,
                SortOrder = sortOrder++,
                IsRequired = r.IsRequired
            });
        }
        sortOrder = 0;
        foreach (var fr in request.FieldRules)
        {
            if (string.IsNullOrWhiteSpace(fr.EntityPath)) continue;
            db.OrderTransactionFieldRules.Add(new OrderTransactionFieldRule
            {
                Id = Guid.NewGuid(),
                OrderTransactionId = t.Id,
                EntityPath = fr.EntityPath,
                ValueSource = fr.ValueSource,
                FixedValue = fr.ValueSource == TransactionFieldValueSourceEnum.Fixed ? fr.FixedValue : null,
                SortOrder = sortOrder++,
                IsRequired = fr.IsRequired
            });
        }
        await db.SaveChangesAsync(ct);

        return t;
    }

    /// <summary>Обновляет документ заказа (полная замена правил).</summary>
    public static async Task<bool> UpdateOrderTransactionAsync(
        this TenantDbContext db,
        Guid id,
        SaveOrderTransactionRequest request,
        CancellationToken ct = default)
    {
        var t = await db.OrderTransactions
            .Include(x => x.Rules)
            .Include(x => x.FieldRules)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (t == null) return false;

        t.Name = request.Name.Trim();
        t.FromSystemStatusId = request.FromSystemStatusId;
        t.ToSystemStatusId = request.ToSystemStatusId;
        t.IsEnabled = request.IsEnabled;
        t.Color = request.Color;
        t.Icon = string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon.Trim();

        db.OrderTransactionRules.RemoveRange(t.Rules);
        db.OrderTransactionFieldRules.RemoveRange(t.FieldRules);

        var sortOrder = 0;
        foreach (var r in request.Rules)
        {
            db.OrderTransactionRules.Add(new OrderTransactionRule
            {
                Id = Guid.NewGuid(),
                OrderTransactionId = t.Id,
                PriceTypeId = r.PriceTypeId,
                ValueSource = r.ValueSource,
                Formula = r.ValueSource == TransactionValueSourceEnum.Formula ? r.Formula : null,
                ProductId = r.ProductId,
                SortOrder = sortOrder++,
                IsRequired = r.IsRequired
            });
        }
        sortOrder = 0;
        foreach (var fr in request.FieldRules)
        {
            if (string.IsNullOrWhiteSpace(fr.EntityPath)) continue;
            db.OrderTransactionFieldRules.Add(new OrderTransactionFieldRule
            {
                Id = Guid.NewGuid(),
                OrderTransactionId = t.Id,
                EntityPath = fr.EntityPath,
                ValueSource = fr.ValueSource,
                FixedValue = fr.ValueSource == TransactionFieldValueSourceEnum.Fixed ? fr.FixedValue : null,
                SortOrder = sortOrder++,
                IsRequired = fr.IsRequired
            });
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Мягкое удаление документа (IsDeleted = true).</summary>
    public static async Task<bool> SoftDeleteOrderTransactionAsync(
        this TenantDbContext db,
        Guid id,
        CancellationToken ct = default)
    {
        var entity = await db.OrderTransactions.FindAsync(new object[] { id }, ct);
        if (entity == null) return false;
        entity.IsDeleted = true;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

