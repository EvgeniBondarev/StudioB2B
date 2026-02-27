using StudioB2B.Domain.Entities.Marketplace;

namespace StudioB2B.Application.Common.Interfaces;

/// <summary>
/// Adapter that synchronises orders from a specific marketplace into the tenant database.
/// </summary>
public interface IOrderAdapter
{
    string MarketplaceName { get; }

    Task SyncAsync(MarketplaceClient client, DateTime cutoffFrom, DateTime cutoffTo, CancellationToken ct = default);
}
