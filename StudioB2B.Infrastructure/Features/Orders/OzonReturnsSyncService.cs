using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioB2B.Domain.Entities.Orders;
using StudioB2B.Infrastructure.Integrations.Ozon;
using StudioB2B.Infrastructure.Integrations.Ozon.Models.Returns;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.Features.Orders;

/// <summary>
/// Загружает возвраты из Ozon API (/v1/returns/list) и сохраняет через upsert по OzonReturnId.
/// После upsert проставляет Order.HasReturn = true для всех связанных заказов.
/// </summary>
public class OzonReturnsSyncService
{
    private readonly TenantDbContext _db;
    private readonly IOzonApiClient _api;
    private readonly ILogger<OzonReturnsSyncService> _logger;

    public OzonReturnsSyncService(
        TenantDbContext db,
        IOzonApiClient api,
        ILogger<OzonReturnsSyncService> logger)
    {
        _db     = db;
        _api    = api;
        _logger = logger;
    }

    public async Task<ReturnsSyncResult> SyncAllAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        var clients = await _db.MarketplaceClients!
            .Include(c => c.ClientType)
            .Where(c => c.ClientType!.Name == "Ozon")
            .ToListAsync(ct);

        _logger.LogInformation("Starting returns sync for {Count} Ozon client(s).", clients.Count);

        var total = new ReturnsSyncResult();

        foreach (var client in clients)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var clientResult = await SyncClientAsync(client.ApiId, client.Key, client.Name, from, to, ct);
                total.Created += clientResult.Created;
                total.Updated += clientResult.Updated;
                total.Linked  += clientResult.Linked;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Returns sync failed for client {ClientId} ({Name}). Skipping.",
                    client.ApiId, client.Name);
            }
        }

        _logger.LogInformation(
            "Returns sync completed: created={Created}, updated={Updated}, linked={Linked}.",
            total.Created, total.Updated, total.Linked);
        return total;
    }

    private async Task<ReturnsSyncResult> SyncClientAsync(
        string clientId,
        string encryptedApiKey,
        string clientName,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        var allReturns = await FetchAllReturnsAsync(clientId, encryptedApiKey, from, to, ct);

        _logger.LogInformation("Client {ClientName}: fetched {Count} return(s) from Ozon.",
            clientName, allReturns.Count);

        if (allReturns.Count == 0)
            return new ReturnsSyncResult();

        var result = new ReturnsSyncResult();

        // Batch-load all existing returns in a single query to avoid N+1 round trips.
        var allIds = allReturns.Select(d => d.Id).ToList();
        var existingMap = await _db.OrderReturns
            .Where(r => allIds.Contains(r.OzonReturnId))
            .ToDictionaryAsync(r => r.OzonReturnId, ct);

        _db.DeferAudit = true;
        try
        {
            foreach (var dto in allReturns)
            {
                if (existingMap.TryGetValue(dto.Id, out var existing))
                {
                    UpdateEntity(existing, dto);
                    result.Updated++;
                }
                else
                {
                    _db.OrderReturns.Add(MapToEntity(dto));
                    result.Created++;
                }
            }

            await _db.SaveChangesAsync(ct);
            await LinkReturnsToOrdersAsync(allReturns, result, ct);
        }
        finally
        {
            _db.DeferAudit = false;
        }

        await _db.FlushDeferredAuditAsync(ct: ct);
        return result;
    }

    private async Task<List<OzonReturnDto>> FetchAllReturnsAsync(
        string clientId,
        string encryptedApiKey,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        var result = new List<OzonReturnDto>();
        long lastId = 0;

        do
        {
            var request = new OzonReturnsListRequest
            {
                Limit  = 500,
                LastId = lastId,
                Filter = new OzonReturnsFilter
                {
                    LogisticReturnDate = new OzonReturnsDateFilter { TimeFrom = from, TimeTo = to }
                }
            };

            var apiResult = await _api.GetReturnsListAsync(clientId, encryptedApiKey, request, ct);

            if (!apiResult.IsSuccess || apiResult.Data == null)
            {
                _logger.LogWarning("GetReturnsList failed for client {ClientId}: {Error}",
                    clientId, apiResult.ErrorMessage);
                break;
            }

            result.AddRange(apiResult.Data.Returns);

            if (!apiResult.Data.HasNext)
                break;

            lastId = apiResult.Data.Returns.Count > 0
                ? apiResult.Data.Returns[^1].Id
                : 0;
        }
        while (true);

        return result;
    }

    /// <summary>
    /// Проставляет Order.HasReturn = true и заполняет OzonReturn.OrderId
    /// для всех заказов, у которых OzonOrderId совпадает с order_id из возвратов.
    /// </summary>
    private async Task LinkReturnsToOrdersAsync(
        List<OzonReturnDto> dtos,
        ReturnsSyncResult stats,
        CancellationToken ct)
    {
        var ozonOrderIds = dtos
            .Where(d => d.OrderId.HasValue)
            .Select(d => d.OrderId!.Value)
            .Distinct()
            .ToList();

        if (ozonOrderIds.Count == 0) return;

        var returnIds = dtos.Select(d => d.Id).ToList();
        var returns = await _db.OrderReturns
            .Where(r => returnIds.Contains(r.OzonReturnId))
            .ToListAsync(ct);

        var orders = await _db.Orders
            .Where(o => o.OzonOrderId.HasValue && ozonOrderIds.Contains(o.OzonOrderId!.Value))
            .ToListAsync(ct);

        foreach (var order in orders)
        {
            if (!order.HasReturn)
            {
                order.HasReturn = true;
                stats.Linked++;
            }

            foreach (var ret in returns.Where(r => r.OzonOrderId == order.OzonOrderId && r.OrderId == null))
            {
                ret.OrderId = order.Id;
            }
        }

        if (orders.Count > 0 || returns.Any(r => r.OrderId != null))
            await _db.SaveChangesAsync(ct);
    }

    private static OrderReturn MapToEntity(OzonReturnDto dto) => new()
    {
        Id                              = Guid.NewGuid(),
        OzonReturnId                    = dto.Id,
        OzonOrderId                     = dto.OrderId,
        OrderNumber                     = dto.OrderNumber,
        PostingNumber                   = dto.PostingNumber,
        SourceId                        = dto.SourceId,
        ClearingId                      = dto.ClearingId,
        ReturnClearingId                = dto.ReturnClearingId,
        ReturnReasonName                = dto.ReturnReasonName,
        Type                            = dto.Type,
        Schema                          = dto.Schema,
        ProductSku                      = dto.Product?.Sku,
        OfferId                         = dto.Product?.OfferId,
        ProductName                     = dto.Product?.Name,
        ProductPrice                    = dto.Product?.Price?.Price,
        ProductPriceCurrencyCode        = dto.Product?.Price?.CurrencyCode,
        ProductPriceWithoutCommission   = dto.Product?.PriceWithoutCommission?.Price,
        CommissionPercent               = dto.Product?.CommissionPercent,
        Commission                      = dto.Product?.Commission?.Price,
        ProductQuantity                 = dto.Product?.Quantity ?? 0,
        VisualStatusId                  = dto.Visual?.Status?.Id,
        VisualStatusDisplayName         = dto.Visual?.Status?.DisplayName,
        VisualStatusSysName             = dto.Visual?.Status?.SysName,
        VisualStatusChangeMoment        = dto.Visual?.ChangeMoment,
        ReturnDate                      = dto.Logistic?.ReturnDate,
        TechnicalReturnMoment           = dto.Logistic?.TechnicalReturnMoment,
        FinalMoment                     = dto.Logistic?.FinalMoment,
        CancelledWithCompensationMoment = dto.Logistic?.CancelledWithCompensationMoment,
        LogisticBarcode                 = dto.Logistic?.Barcode,
        StorageSum                      = dto.Storage?.Sum?.Price,
        StorageCurrencyCode             = dto.Storage?.Sum?.CurrencyCode,
        StorageTariffStartDate          = dto.Storage?.TariffStartDate,
        StorageArrivedMoment            = dto.Storage?.ArrivedMoment,
        StorageDays                     = dto.Storage?.Days,
        UtilizationSum                  = dto.Storage?.UtilizationSum?.Price,
        UtilizationForecastDate         = dto.Storage?.UtilizationForecastDate,
        PlaceName                       = dto.Place?.Name,
        PlaceAddress                    = dto.Place?.Address,
        CompensationStatusId            = dto.CompensationStatus?.Status?.Id,
        CompensationStatusDisplayName   = dto.CompensationStatus?.Status?.DisplayName,
        CompensationStatusChangeMoment  = dto.CompensationStatus?.ChangeMoment,
        IsOpened                        = dto.AdditionalInfo?.IsOpened ?? false,
        IsSuperEconom                   = dto.AdditionalInfo?.IsSuperEconom ?? false,
        SyncedAt                        = DateTime.UtcNow
    };

    private static void UpdateEntity(OrderReturn entity, OzonReturnDto dto)
    {
        entity.OzonOrderId                      = dto.OrderId;
        entity.OrderNumber                      = dto.OrderNumber;
        entity.PostingNumber                    = dto.PostingNumber;
        entity.ReturnReasonName                 = dto.ReturnReasonName;
        entity.Type                             = dto.Type;
        entity.Schema                           = dto.Schema;
        entity.ProductSku                       = dto.Product?.Sku;
        entity.OfferId                          = dto.Product?.OfferId;
        entity.ProductName                      = dto.Product?.Name;
        entity.ProductPrice                     = dto.Product?.Price?.Price;
        entity.ProductPriceCurrencyCode         = dto.Product?.Price?.CurrencyCode;
        entity.ProductPriceWithoutCommission    = dto.Product?.PriceWithoutCommission?.Price;
        entity.CommissionPercent                = dto.Product?.CommissionPercent;
        entity.Commission                       = dto.Product?.Commission?.Price;
        entity.ProductQuantity                  = dto.Product?.Quantity ?? 0;
        entity.VisualStatusId                   = dto.Visual?.Status?.Id;
        entity.VisualStatusDisplayName          = dto.Visual?.Status?.DisplayName;
        entity.VisualStatusSysName              = dto.Visual?.Status?.SysName;
        entity.VisualStatusChangeMoment         = dto.Visual?.ChangeMoment;
        entity.ReturnDate                       = dto.Logistic?.ReturnDate;
        entity.TechnicalReturnMoment            = dto.Logistic?.TechnicalReturnMoment;
        entity.FinalMoment                      = dto.Logistic?.FinalMoment;
        entity.CancelledWithCompensationMoment  = dto.Logistic?.CancelledWithCompensationMoment;
        entity.LogisticBarcode                  = dto.Logistic?.Barcode;
        entity.StorageSum                       = dto.Storage?.Sum?.Price;
        entity.StorageCurrencyCode              = dto.Storage?.Sum?.CurrencyCode;
        entity.StorageTariffStartDate           = dto.Storage?.TariffStartDate;
        entity.StorageArrivedMoment             = dto.Storage?.ArrivedMoment;
        entity.StorageDays                      = dto.Storage?.Days;
        entity.UtilizationSum                   = dto.Storage?.UtilizationSum?.Price;
        entity.UtilizationForecastDate          = dto.Storage?.UtilizationForecastDate;
        entity.PlaceName                        = dto.Place?.Name;
        entity.PlaceAddress                     = dto.Place?.Address;
        entity.CompensationStatusId             = dto.CompensationStatus?.Status?.Id;
        entity.CompensationStatusDisplayName    = dto.CompensationStatus?.Status?.DisplayName;
        entity.CompensationStatusChangeMoment   = dto.CompensationStatus?.ChangeMoment;
        entity.IsOpened                         = dto.AdditionalInfo?.IsOpened ?? false;
        entity.IsSuperEconom                    = dto.AdditionalInfo?.IsSuperEconom ?? false;
        entity.SyncedAt                         = DateTime.UtcNow;
    }
}

public class ReturnsSyncResult
{
    public int Created { get; set; }
    public int Updated { get; set; }
    /// <summary>Заказов, которым проставлен HasReturn = true.</summary>
    public int Linked  { get; set; }
}
