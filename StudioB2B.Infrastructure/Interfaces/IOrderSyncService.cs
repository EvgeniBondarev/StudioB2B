using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Orchestrates order synchronisation for all marketplace clients.
/// </summary>
public interface IOrderSyncService
{
    Task<OrderSyncSummary> SyncAllAsync(DateTime cutoffFrom, DateTime cutoffTo, CancellationToken ct = default);

    /// <summary>
    /// Updates statuses and dates for all active (non-terminal) shipments across all Ozon FBS clients.
    /// </summary>
    Task<OrderSyncSummary> UpdateAllAsync(CancellationToken ct = default);
}
