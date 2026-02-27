using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.Services;

public class OrderSyncService : IOrderSyncService
{
    private readonly TenantDbContext _db;
    private readonly IOrderAdapter _adapter;
    private readonly ILogger<OrderSyncService> _logger;

    public OrderSyncService(
        TenantDbContext db,
        IOrderAdapter adapter,
        ILogger<OrderSyncService> logger)
    {
        _db = db;
        _adapter = adapter;
        _logger = logger;
    }

    public async Task SyncAllAsync(DateTime cutoffFrom, DateTime cutoffTo, CancellationToken ct = default)
    {
        var ozonClients = await _db.MarketplaceClients!
            .Include(c => c.ClientType)
            .Include(c => c.Mode)
            .Where(c => c.ClientType!.Name == "Ozon" && c.Mode!.Name == "FBS")
            .ToListAsync(ct);

        _logger.LogInformation(
            "Starting order sync for {Count} Ozon FBS client(s).", ozonClients.Count);

        foreach (var client in ozonClients)
        {
            try
            {
                _logger.LogInformation(
                    "Syncing orders for client {ClientId} ({ClientName}) in period {From}–{To}.",
                    client.ApiId, client.Name, cutoffFrom, cutoffTo);
                await _adapter.SyncAsync(client, cutoffFrom, cutoffTo, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled error while syncing client {ClientId} ({ClientName}). Skipping.",
                    client.ApiId, client.Name);
            }
        }

        _logger.LogInformation("Order sync completed.");
    }
}
