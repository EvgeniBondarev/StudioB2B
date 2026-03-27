using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Orchestrates order synchronisation for all marketplace clients.
/// </summary>
public interface IOrderSyncService
{
    /// <param name="allowedClientIds">
    /// When non-null, only clients whose IDs are in this set are synced.
    /// Pass <c>null</c> to sync all clients (no restriction).
    /// </param>
    Task<OrderSyncSummaryDto> SyncAllAsync(DateTime cutoffFrom, DateTime cutoffTo, CancellationToken ct = default,
        Func<string, Task>? onProgress = null, HashSet<Guid>? allowedClientIds = null);

    /// <summary>
    /// Updates statuses and dates for active (non-terminal) shipments across all Ozon FBS clients
    /// within the specified date range.
    /// </summary>
    /// <param name="allowedClientIds">
    /// When non-null, only clients whose IDs are in this set are updated.
    /// Pass <c>null</c> to update all clients (no restriction).
    /// </param>
    Task<OrderSyncSummaryDto> UpdateAllAsync(DateTime from, DateTime to, CancellationToken ct = default,
        Func<string, Task>? onProgress = null, HashSet<Guid>? allowedClientIds = null);

    /// <summary>
    /// Updates the status of a single shipment by its ID.
    /// </summary>
    Task<ShipmentUpdateItemDto?> UpdateSingleShipmentStatusAsync(Guid shipmentId, CancellationToken ct = default);
}
