using StudioB2B.Application.Common;
using StudioB2B.Domain.Entities.Marketplace;

namespace StudioB2B.Application.Common.Interfaces;

/// <summary>
/// Adapter that synchronises orders from a specific marketplace into the tenant database.
/// </summary>
public interface IOrderAdapter
{
    string MarketplaceName { get; }

    Task<OrderSyncResult> SyncAsync(MarketplaceClient client, DateTime cutoffFrom, DateTime cutoffTo, CancellationToken ct = default);

    /// <summary>
    /// Updates statuses and dates for all active (non-terminal) shipments of the given client
    /// by fetching each posting from the marketplace API.
    /// </summary>
    Task<OrderSyncResult> UpdateStatusesAsync(MarketplaceClient client, CancellationToken ct = default);
}
