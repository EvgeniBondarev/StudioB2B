using StudioB2B.Application.Common;

namespace StudioB2B.Application.Common.Interfaces;

/// <summary>
/// Orchestrates order synchronisation for all marketplace clients.
/// </summary>
public interface IOrderSyncService
{
    Task<OrderSyncResult> SyncAllAsync(DateTime cutoffFrom, DateTime cutoffTo, CancellationToken ct = default);
}
