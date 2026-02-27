namespace StudioB2B.Application.Common.Interfaces;

/// <summary>
/// Orchestrates order synchronisation for all marketplace clients.
/// </summary>
public interface IOrderSyncService
{
    Task SyncAllAsync(DateTime cutoffFrom, DateTime cutoffTo, CancellationToken ct = default);
}
