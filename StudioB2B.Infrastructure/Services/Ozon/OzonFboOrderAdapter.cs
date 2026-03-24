using Microsoft.Extensions.Logging;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Services.Ozon;

/// <summary>
/// Adapter for Ozon shipments/orders in delivery scheme FBO.
/// Implementation reuses the existing upsert logic from <see cref="OzonFbsOrderAdapter"/>
/// by mapping FBO posting DTOs into the FBS-shaped posting DTOs.
/// </summary>
public class OzonFboOrderAdapter : IOrderAdapter
{
    private readonly IOzonApiClient _api;
    private readonly OzonFbsOrderAdapter _inner;

    public OzonFboOrderAdapter(
        IOzonApiClient api,
        TenantDbContext db,
        ILogger<OzonFboOrderAdapter> _,
        ILogger<OzonFbsOrderAdapter> fbsLogger,
        IModuleService moduleService)
    {
        _api = api;

        // Reuse the same upsert pipeline, but switch delivery scheme and posting-fetching strategy.
        _inner = new OzonFbsOrderAdapter(
            api,
            db,
            fbsLogger,
            moduleService,
            deliverySchemeCode: "fbo",
            fetchAllPostingsAsync: FetchAllPostingsForFboAsync,
            fetchPostingForStatusAsync: FetchPostingForFboStatusAsync);
    }

    public string MarketplaceName => "Ozon";

    public string ClientModeName => "FBO";

    public Task<OrderSyncResultDto> SyncAsync(
        MarketplaceClient client,
        DateTime cutoffFrom,
        DateTime cutoffTo,
        CancellationToken ct = default) =>
        _inner.SyncAsync(client, cutoffFrom, cutoffTo, ct);

    public Task<OrderSyncResultDto> UpdateStatusesAsync(
        MarketplaceClient client,
        DateTime from,
        DateTime to,
        CancellationToken ct = default) =>
        _inner.UpdateStatusesAsync(client, from, to, ct);

    public Task<ShipmentUpdateItemDto?> UpdateSingleShipmentStatusAsync(
        Shipment shipment,
        MarketplaceClient client,
        CancellationToken ct = default) =>
        _inner.UpdateSingleShipmentStatusAsync(shipment, client, ct);

    private async Task<List<OzonFbsPostingDto>> FetchAllPostingsForFboAsync(
        MarketplaceClient client,
        DateTime cutoffFrom,
        DateTime cutoffTo,
        CancellationToken ct)
    {
        const int limit = 100;
        var result = new List<OzonFbsPostingDto>();
        var offset = 0;

        while (true)
        {
            var apiResult = await _api.GetFboPostingListAsync(
                client.ApiId, client.Key,
                cutoffFrom, cutoffTo,
                limit, offset, ct);

            if (!apiResult.IsSuccess || apiResult.Data?.Result == null)
                break;

            var postings = apiResult.Data.Result;
            if (postings.Count == 0)
                break;

            foreach (var fboPosting in postings)
                result.Add(MapToFbsPosting(fboPosting));

            offset += limit;
            if (postings.Count < limit)
                break;
        }

        return result;
    }

    private async Task<OzonFbsPostingDto?> FetchPostingForFboStatusAsync(
        MarketplaceClient client,
        string postingNumber,
        CancellationToken ct)
    {
        var apiResult = await _api.GetFboPostingAsync(client.ApiId, client.Key, postingNumber, ct);
        if (!apiResult.IsSuccess || apiResult.Data?.Result == null)
            return null;

        return MapToFbsPosting(apiResult.Data.Result);
    }

    private static OzonFbsPostingDto MapToFbsPosting(OzonFboPostingDto fbo)
    {
        OzonFbsCustomerDto? customer = null;
        if (!string.IsNullOrWhiteSpace(fbo.LegalInfo?.CompanyName))
        {
            customer = new OzonFbsCustomerDto { Name = fbo.LegalInfo!.CompanyName };
        }

        return new OzonFbsPostingDto
        {
            PostingNumber = fbo.PostingNumber,
            OrderId = fbo.OrderId,
            OrderNumber = fbo.OrderNumber,
            Status = fbo.Status,
            TrackingNumber = fbo.TrackingNumber, // usually null for FBO
            InProcessAt = fbo.InProcessAt,
            ShipmentDate = fbo.ShipmentDate,
            DeliveryMethod = fbo.DeliveryMethod == null
                ? null
                : new OzonFbsDeliveryMethodDto
                {
                    Id = fbo.DeliveryMethod.Id,
                    Name = fbo.DeliveryMethod.Name,
                    WarehouseId = fbo.DeliveryMethod.WarehouseId,
                    Warehouse = fbo.DeliveryMethod.Warehouse
                },
            Products = fbo.Products,
            Customer = customer,
            FinancialData = fbo.FinancialData
        };
    }
}

