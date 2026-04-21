using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Services.Order;

public class OrderSyncService : IOrderSyncService
{
    private readonly TenantDbContext _db;
    private readonly Dictionary<string, IOrderAdapter> _adaptersByMode;
    private readonly ILogger<OrderSyncService> _logger;

    public OrderSyncService(TenantDbContext db,
                            IEnumerable<IOrderAdapter> adapters,
                            ILogger<OrderSyncService> logger)
    {
        _db = db;
        _adaptersByMode = adapters.ToDictionary(a => a.ClientModeName, a => a, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    public async Task<OrderSyncSummaryDto> SyncAllAsync(DateTime cutoffFrom, DateTime cutoffTo,
                                                        CancellationToken ct = default, Func<string, Task>? onProgress = null,
                                                        HashSet<Guid>? allowedClientIds = null)
    {
        var query = _db.MarketplaceClients!
            .Include(c => c.ClientType)
            .Include(c => c.Mode)
            .Include(c => c.Mode2)
            .Where(c => c.ClientType!.Name == "Ozon" && (c.Mode != null || c.Mode2 != null));

        if (allowedClientIds is not null)
            query = query.Where(c => allowedClientIds.Contains(c.Id));

        var ozonClients = await query.ToListAsync(ct);

        _logger.LogWarning(
            "Starting order sync for {Count} Ozon client(s) in FBS/FBO.", ozonClients.Count);

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

                if (client.Mode != null && _adaptersByMode.TryGetValue(client.Mode.Name, out var adapter))
                {
                    var clientResult = await adapter.SyncAsync(client, cutoffFrom, cutoffTo, ct);
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

                if (client.Mode2 != null && _adaptersByMode.TryGetValue(client.Mode2.Name, out var adapter2))
                {
                    var clientResult = await adapter2.SyncAsync(client, cutoffFrom, cutoffTo, ct);
                    summary.Total.Add(clientResult);
                    summary.PerClient.Add(new ClientSyncResultItemDto
                    {
                        ClientName = client.Name,
                        Mode = client.Mode2?.Name ?? string.Empty,
                        ShipmentsCreated = clientResult.ShipmentsCreated,
                        ShipmentsUpdated = clientResult.ShipmentsUpdated,
                        ShipmentsUntouched = clientResult.ShipmentsUntouched,
                        OrdersCreated = clientResult.OrdersCreated,
                        OrdersUpdated = clientResult.OrdersUpdated,
                        OrdersUntouched = clientResult.OrdersUntouched
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled error while syncing client {ClientId} ({ClientName}). Skipping.",
                    client.ApiId, client.Name);
            }
        }

        _logger.LogWarning("Order sync completed: {Clients} client(s) processed.",
            summary.PerClient.Count);
        return summary;
    }

    public async Task<OrderSyncSummaryDto> UpdateAllAsync(DateTime from, DateTime to, CancellationToken ct = default,
        Func<string, Task>? onProgress = null, HashSet<Guid>? allowedClientIds = null)
    {
        var query = _db.MarketplaceClients!
            .Include(c => c.ClientType)
            .Include(c => c.Mode)
            .Include(c => c.Mode2)
            .Where(c => c.ClientType!.Name == "Ozon" && (c.Mode != null || c.Mode2 != null));

        if (allowedClientIds is not null)
            query = query.Where(c => allowedClientIds.Contains(c.Id));

        var ozonClients = await query.ToListAsync(ct);

        _logger.LogWarning(
            "Starting status update for {Count} Ozon clients (FBS/FBO), period {From}\u2013{To}.",
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

                if (client.Mode != null && _adaptersByMode.TryGetValue(client.Mode.Name, out var adapter))
                {
                    var clientResult = await adapter.UpdateStatusesAsync(client, from, to, ct);
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

                if (client.Mode2 != null && _adaptersByMode.TryGetValue(client.Mode2.Name, out var adapter2))
                {
                    var clientResult = await adapter2.UpdateStatusesAsync(client, from, to, ct);
                    summary.Total.Add(clientResult);
                    summary.UpdatedShipments.AddRange(clientResult.UpdatedShipments);
                    summary.PerClient.Add(new ClientSyncResultItemDto
                    {
                        ClientName = client.Name,
                        Mode = client.Mode2?.Name ?? string.Empty,
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled error while updating statuses for client {ClientId} ({ClientName}). Skipping.",
                    client.ApiId, client.Name);
            }
        }

        _logger.LogWarning("Status update completed: {Clients} client(s) processed.",
            summary.PerClient.Count);
        return summary;
    }

    public async Task<ShipmentUpdateItemDto?> UpdateSingleShipmentStatusAsync(Guid shipmentId, CancellationToken ct = default)
    {
        var shipment = await _db.Shipments
            .Include(s => s.Status)
            .Include(s => s.MarketplaceClient)
                .ThenInclude(c => c.ClientType)
            .Include(s => s.DeliveryMethod)
                .ThenInclude(dm => dm!.DeliveryType)
            .FirstOrDefaultAsync(s => s.Id == shipmentId, ct);

        if (shipment is null)
        {
            _logger.LogWarning("Shipment {ShipmentId} not found.", shipmentId);
            return null;
        }

        var client = shipment.MarketplaceClient;
        if (client.ClientType?.Name != "Ozon")
        {
            _logger.LogWarning("Shipment {ShipmentId} belongs to unsupported client type.", shipmentId);
            return null;
        }

        var schemeName = shipment.DeliveryMethod?.DeliveryType?.Name;
        if (schemeName == null || !_adaptersByMode.TryGetValue(schemeName, out var adapter))
        {
            _logger.LogWarning("Shipment {ShipmentId} belongs to unsupported delivery mode.", shipmentId);
            return null;
        }

        return await adapter.UpdateSingleShipmentStatusAsync(shipment, client, ct);
    }
}
