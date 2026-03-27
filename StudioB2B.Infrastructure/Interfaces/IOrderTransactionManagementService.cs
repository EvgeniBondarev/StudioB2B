using StudioB2B.Domain.Entities;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// CRUD-сервис для управления документами заказов (создание/редактирование/удаление).
/// Не путать с IOrderTransactionService, который отвечает за проведение документов.
/// </summary>
public interface IOrderTransactionManagementService
{
    Task<(List<OrderTransaction> Items, int TotalCount)> GetPagedAsync(
        string? dynamicFilter, string? orderBy, int skip, int take, CancellationToken ct = default);

    Task<List<OrderTransaction>> GetAllEnabledAsync(CancellationToken ct = default);

    Task<(List<OrderStatus> Statuses, List<OrderTransaction> Transactions)> GetCanvasDataAsync(
        CancellationToken ct = default);

    Task<List<OrderTransaction>> GetAllForCanvasAsync(CancellationToken ct = default);

    Task<OrderTransaction?> GetForEditAsync(Guid id, CancellationToken ct = default);

    Task<TransactionEditReferenceData> GetEditReferenceDataAsync(CancellationToken ct = default);

    Task<(List<OrderTransactionHistory> Items, int TotalCount)> GetHistoryPagedAsync(
        string? dynamicFilter, string? orderBy, int skip, int take, CancellationToken ct = default);

    Task<OrderTransaction> CreateAsync(SaveOrderTransactionRequest request, CancellationToken ct = default);

    Task<bool> UpdateAsync(Guid id, SaveOrderTransactionRequest request, CancellationToken ct = default);

    Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct = default);
}
