using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Shared.DTOs;
using IOrderAdapter = StudioB2B.Infrastructure.Interfaces.IOrderAdapter;
using IOrderSyncService = StudioB2B.Infrastructure.Interfaces.IOrderSyncService;

namespace StudioB2B.Infrastructure.Services.Order;

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

    public async Task<OrderSyncSummaryDto> SyncAllAsync(DateTime cutoffFrom, DateTime cutoffTo,
        CancellationToken ct = default, Func<string, Task>? onProgress = null)
    {
        var ozonClients = await _db.MarketplaceClients!
            .Include(c => c.ClientType)
            .Include(c => c.Mode)
            .Where(c => c.ClientType!.Name == "Ozon" && c.Mode!.Name == "FBS")
            .ToListAsync(ct);

        _logger.LogInformation(
            "Starting order sync for {Count} Ozon FBS client(s).", ozonClients.Count);

        var summary = new OrderSyncSummaryDto();
        var total = ozonClients.Count;

        for (var idx = 0; idx < ozonClients.Count; idx++)
        {
            var client = ozonClients[idx];
            if (onProgress is not null)
                await onProgress($"Клиент {idx + 1}/{total}: {client.Name}");
            try
            {
                _logger.LogInformation(
                    "Syncing orders for client {ClientId} ({ClientName}) in period {From}–{To}.",
                    client.ApiId, client.Name, cutoffFrom, cutoffTo);
                var clientResult = await _adapter.SyncAsync(client, cutoffFrom, cutoffTo, ct);
                summary.Total.Add(clientResult);
                summary.PerClient.Add(new ClientSyncResultItemDto
                {
                    ClientName = client.Name,
                    Mode = client.Mode?.Name ?? string.Empty,
                    ShipmentsCreated = clientResult.ShipmentsCreated,
                    ShipmentsUpdated = clientResult.ShipmentsUpdated,
                    ShipmentsUntouched = clientResult.ShipmentsUntouched,
                    OrdersCreated = clientResult.OrdersCreated,
                    OrdersUpdated = clientResult.OrdersUpdated,
                    OrdersUntouched = clientResult.OrdersUntouched
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled error while syncing client {ClientId} ({ClientName}). Skipping.",
                    client.ApiId, client.Name);
            }
        }

        _logger.LogInformation("Order sync completed.");
        return summary;
    }

    public async Task<OrderSyncSummaryDto> UpdateAllAsync(DateTime from, DateTime to, CancellationToken ct = default,
        Func<string, Task>? onProgress = null)
    {
        var ozonClients = await _db.MarketplaceClients!
            .Include(c => c.ClientType)
            .Include(c => c.Mode)
            .Where(c => c.ClientType!.Name == "Ozon" && c.Mode!.Name == "FBS")
            .ToListAsync(ct);

        _logger.LogInformation(
            "Starting status update for {Count} Ozon FBS client(s), period {From}\u2013{To}.",
            ozonClients.Count, from, to);

        var summary = new OrderSyncSummaryDto();
        var total = ozonClients.Count;

        for (var idx = 0; idx < ozonClients.Count; idx++)
        {
            var client = ozonClients[idx];
            ct.ThrowIfCancellationRequested();
            if (onProgress is not null)
                await onProgress($"\u041a\u043b\u0438\u0435\u043d\u0442 {idx + 1}/{total}: {client.Name}");
            try
            {
                _logger.LogInformation(
                    "Updating statuses for client {ClientId} ({ClientName}).",
                    client.ApiId, client.Name);

                var clientResult = await _adapter.UpdateStatusesAsync(client, from, to, ct);
                summary.Total.Add(clientResult);
                summary.UpdatedShipments.AddRange(clientResult.UpdatedShipments);
                summary.PerClient.Add(new ClientSyncResultItemDto
                {
                    ClientName = client.Name,
                    Mode = client.Mode?.Name ?? string.Empty,
                    ShipmentsCreated = clientResult.ShipmentsCreated,
                    ShipmentsUpdated = clientResult.ShipmentsUpdated,
                    ShipmentsUntouched = clientResult.ShipmentsUntouched,
                    OrdersCreated = clientResult.OrdersCreated,
                    OrdersUpdated = clientResult.OrdersUpdated,
                    OrdersUntouched = clientResult.OrdersUntouched,
                    OrdersSelectedForUpdate = clientResult.OrdersSelectedForUpdate,
                    OrdersSkipped = clientResult.OrdersSkipped,
                    UpdatedFieldsSummary = clientResult.UpdatedFieldsSummary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled error while updating statuses for client {ClientId} ({ClientName}). Skipping.",
                    client.ApiId, client.Name);
            }
        }

        _logger.LogInformation("Status update completed.");
        return summary;
    }

    public async Task<ShipmentUpdateItemDto?> UpdateSingleShipmentStatusAsync(Guid shipmentId, CancellationToken ct = default)
    {
        var shipment = await _db.Shipments
            .Include(s => s.Status)
            .Include(s => s.MarketplaceClient)
                .ThenInclude(c => c.ClientType)
            .Include(s => s.MarketplaceClient)
                .ThenInclude(c => c.Mode)
            .FirstOrDefaultAsync(s => s.Id == shipmentId, ct);

        if (shipment is null)
        {
            _logger.LogWarning("Shipment {ShipmentId} not found.", shipmentId);
            return null;
        }

        var client = shipment.MarketplaceClient;
        if (client.ClientType?.Name != "Ozon" || client.Mode?.Name != "FBS")
        {
            _logger.LogWarning("Shipment {ShipmentId} belongs to unsupported client type.", shipmentId);
            return null;
        }

        return await _adapter.UpdateSingleShipmentStatusAsync(shipment, client, ct);
    }
}
