using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Orchestrates order synchronisation for all marketplace clients.
/// </summary>
public interface IOrderSyncService
{
    Task<OrderSyncSummary> SyncAllAsync(DateTime cutoffFrom, DateTime cutoffTo, CancellationToken ct = default);

    /// <summary>
    /// Updates statuses and dates for active (non-terminal) shipments across all Ozon FBS clients
    /// within the specified date range.
    /// </summary>
    Task<OrderSyncSummary> UpdateAllAsync(DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>
    /// Updates the status of a single shipment by its ID.
    /// </summary>
    Task<ShipmentUpdateItem?> UpdateSingleShipmentStatusAsync(Guid shipmentId, CancellationToken ct = default);
}
