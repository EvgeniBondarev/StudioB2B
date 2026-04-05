using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Services.Order;

/// <summary>
/// CRUD-сервис для управления документами заказов.
/// Инкапсулирует работу с БД через extension-методы из OrderTransactionFeatures.
/// </summary>
public class OrderTransactionManagementService : IOrderTransactionManagementService
{
    private readonly ITenantDbContextFactory _dbContextFactory;

    public OrderTransactionManagementService(ITenantDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<(List<OrderTransaction> Items, int TotalCount)> GetPagedAsync(
        string? dynamicFilter, string? orderBy, int skip, int take, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetOrderTransactionsPagedAsync(dynamicFilter, orderBy, skip, take, ct);
    }

    public async Task<List<OrderTransaction>> GetAllEnabledAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetEnabledOrderTransactionsWithStatusesAsync(ct);
    }

    public async Task<(List<OrderStatus> Statuses, List<OrderTransaction> Transactions)> GetCanvasDataAsync(
        CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetCanvasDataAsync(ct);
    }

    public async Task<List<OrderTransaction>> GetAllForCanvasAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetAllOrderTransactionsForCanvasAsync(ct);
    }

    public async Task<OrderTransaction?> GetForEditAsync(Guid id, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetOrderTransactionForEditAsync(id, ct);
    }

    public async Task<TransactionEditReferenceData> GetEditReferenceDataAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetTransactionEditReferenceDataAsync(ct);
    }

    public async Task<(List<OrderTransactionHistory> Items, int TotalCount)> GetHistoryPagedAsync(
        string? dynamicFilter, string? orderBy, int skip, int take, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetOrderTransactionHistoryPagedAsync(dynamicFilter, orderBy, skip, take, ct);
    }

    public async Task<OrderTransaction> CreateAsync(SaveOrderTransactionRequest request, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.CreateOrderTransactionAsync(request, ct);
    }

    public async Task<bool> UpdateAsync(Guid id, SaveOrderTransactionRequest request, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.UpdateOrderTransactionAsync(id, request, ct);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.SoftDeleteOrderTransactionAsync(id, ct);
    }
}
