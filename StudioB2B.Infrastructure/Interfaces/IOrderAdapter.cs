using StudioB2B.Domain.Entities;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Adapter that synchronises orders from a specific marketplace into the tenant database.
/// </summary>
public interface IOrderAdapter
{
    string MarketplaceName { get; }
    /// <summary>
    /// Mode name in DB (e.g. "FBS" / "FBO") that this adapter supports.
    /// </summary>
    string ClientModeName { get; }

    Task<OrderSyncResultDto> SyncAsync(MarketplaceClient client, DateTime cutoffFrom, DateTime cutoffTo, CancellationToken ct = default);

    /// <summary>
    /// Updates statuses and dates for active (non-terminal) shipments of the given client
    /// within the specified date range by fetching each posting from the marketplace API.
    /// </summary>
    Task<OrderSyncResultDto> UpdateStatusesAsync(MarketplaceClient client, DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>
    /// Updates the status of a single shipment by fetching its posting from the marketplace API.
    /// </summary>
    Task<ShipmentUpdateItemDto?> UpdateSingleShipmentStatusAsync(Shipment shipment, MarketplaceClient client, CancellationToken ct = default);
}
