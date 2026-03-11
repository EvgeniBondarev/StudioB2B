using StudioB2B.Domain.Entities.Marketplace;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Interfaces;

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
