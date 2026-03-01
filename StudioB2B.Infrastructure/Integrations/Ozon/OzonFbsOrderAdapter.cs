using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using StudioB2B.Application.Common;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Domain.Entities.Marketplace;
using StudioB2B.Domain.Entities.Orders;
using StudioB2B.Domain.Entities.Products;
using StudioB2B.Domain.Entities.References;
using StudioB2B.Domain.Entities.Warehouses;
using StudioB2B.Infrastructure.Integrations.Ozon.Models.FbsUnfulfilled;
using StudioB2B.Infrastructure.Integrations.Ozon.Models.ProductAttributes;
using StudioB2B.Infrastructure.Integrations.Ozon.Models.ProductPrices;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.Integrations.Ozon;

public class OzonFbsOrderAdapter : IOrderAdapter
{
    private readonly IOzonApiClient _api;
    private readonly TenantDbContext _db;
    private readonly ILogger<OzonFbsOrderAdapter> _logger;

    public OzonFbsOrderAdapter(
        IOzonApiClient api,
        TenantDbContext db,
        ILogger<OzonFbsOrderAdapter> logger)
    {
        _api = api;
        _db = db;
        _logger = logger;
    }

    public string MarketplaceName => "Ozon";

    public async Task<OrderSyncResult> SyncAsync(
        MarketplaceClient client,
        DateTime cutoffFrom,
        DateTime cutoffTo,
        CancellationToken ct = default)
    {
        var allPostings = await FetchAllPostingsAsync(client, cutoffFrom, cutoffTo, ct);

        if (allPostings.Count == 0)
        {
            _logger.LogInformation(
                "No FBS postings found for client {ClientId} in the last 30 days.", client.ApiId);
            return new OrderSyncResult();
        }

        var priceByOfferId = await FetchPricesAsync(client, allPostings, ct);
        var attributesByOfferId = await FetchAttributesAsync(client, allPostings, ct);

        var result = new OrderSyncResult();

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            foreach (var posting in allPostings)
            {
                await UpsertShipmentAsync(client, posting, priceByOfferId, attributesByOfferId, result, tx, ct);
            }
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _db.ChangeTracker.Clear();
            _logger.LogError(ex,
                "Sync failed for client {ClientId}, all changes rolled back.",
                client.ApiId);
            throw;
        }

        return result;
    }

    public async Task<OrderSyncResult> UpdateStatusesAsync(
        MarketplaceClient client,
        CancellationToken ct = default)
    {
        var shipments = await _db.Shipments
            .Include(s => s.Status)
            .Where(s => s.MarketplaceClientId == client.Id
                        && (s.StatusId == null || s.Status == null || !s.Status.IsTerminal))
            .ToListAsync(ct);

        _logger.LogInformation(
            "Updating statuses for {Count} active shipments of client {ClientId}.",
            shipments.Count, client.ApiId);

        var result = new OrderSyncResult();

        foreach (var shipment in shipments)
        {
            try
            {
                var apiResult = await _api.GetFbsPostingAsync(client.ApiId, client.Key, shipment.PostingNumber, ct);

                if (!apiResult.IsSuccess || apiResult.Data?.Result == null)
                {
                    _logger.LogWarning(
                        "Failed to fetch posting {PostingNumber} for client {ClientId}: {Error}",
                        shipment.PostingNumber, client.ApiId, apiResult.ErrorMessage);
                    continue;
                }

                await UpdateShipmentStatusAsync(shipment, client.Name, apiResult.Data.Result, result, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error updating shipment {PostingNumber} for client {ClientId}.",
                    shipment.PostingNumber, client.ApiId);
            }
        }

        if (result.ShipmentsUpdated > 0)
            result.UpdatedFieldsSummary = "Статус, Номер заказа, Дата отгрузки, Дата принятия, Трек-номер, Способ доставки";

        return result;
    }

    private async Task UpdateShipmentStatusAsync(
        Shipment shipment,
        string clientName,
        OzonFbsPostingDto posting,
        OrderSyncResult stats,
        CancellationToken ct)
    {
        var oldStatusName = shipment.Status?.Name ?? "—";
        var newStatusName = GetOzonShipmentStatusDisplayName(posting.Status);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var status = await EnsureOrderStatusAsync(posting.Status, ct);
            var deliveryMethod = await EnsureDeliveryMethodAsync(posting, ct);

            shipment.OrderNumber = posting.OrderNumber;
            shipment.StatusId = status.Id;
            shipment.DeliveryMethodId = deliveryMethod?.Id;
            shipment.ShipmentDate = posting.ShipmentDate;
            shipment.TrackingNumber = posting.TrackingNumber;
            shipment.InProcessAt = posting.InProcessAt;

            var orders = await _db.Orders
                .Include(o => o.Status)
                .Where(o => o.ShipmentId == shipment.Id)
                .ToListAsync(ct);

            foreach (var order in orders)
            {
                if (order.StatusId == null || order.Status == null || !order.Status.IsTerminal)
                    order.StatusId = status.Id;
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            stats.ShipmentsUpdated++;
            stats.OrdersUpdated += orders.Count;
            if (oldStatusName != newStatusName)
            {
                stats.UpdatedShipments.Add(new ShipmentUpdateItem
                {
                    PostingNumber = shipment.PostingNumber,
                    ClientName = clientName,
                    OldStatusName = oldStatusName,
                    NewStatusName = newStatusName
                });
            }
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _db.ChangeTracker.Clear();
            _logger.LogError(ex,
                "Failed to update shipment {PostingNumber}, rolled back.",
                shipment.PostingNumber);
            throw;
        }
    }

    private async Task<List<OzonFbsPostingDto>> FetchAllPostingsAsync(
        MarketplaceClient client,
        DateTime cutoffFrom,
        DateTime cutoffTo,
        CancellationToken ct)
    {
        const int limit = 100;
        var result = new List<OzonFbsPostingDto>();
        int offset = 0;
        int totalCount;

        do
        {
            var apiResult = await _api.GetFbsUnfulfilledListAsync(
                client.ApiId, client.Key,
                cutoffFrom, cutoffTo,
                limit, offset, ct);

            if (!apiResult.IsSuccess || apiResult.Data?.Result == null)
            {
                _logger.LogWarning(
                    "Failed to fetch FBS postings for client {ClientId} at offset {Offset}: {Error}",
                    client.ApiId, offset, apiResult.ErrorMessage);
                break;
            }

            totalCount = apiResult.Data.Result.Count;
            result.AddRange(apiResult.Data.Result.Postings);
            offset += limit;
        }
        while (offset < totalCount);

        return result;
    }

    private async Task<Dictionary<string, OzonProductPriceItemDto>> FetchPricesAsync(
        MarketplaceClient client,
        List<OzonFbsPostingDto> postings,
        CancellationToken ct)
    {
        var offerIds = postings
            .SelectMany(p => p.Products)
            .Select(p => p.OfferId)
            .Distinct()
            .ToList();

        var priceByOfferId = new Dictionary<string, OzonProductPriceItemDto>();

        if (offerIds.Count == 0)
            return priceByOfferId;

        var pricesResult = await _api.GetProductPricesAsync(
            client.ApiId, client.Key, offerIds, ct: ct);

        if (!pricesResult.IsSuccess || pricesResult.Data?.Items == null)
        {
            _logger.LogWarning(
                "Failed to fetch product prices for client {ClientId}: {Error}",
                client.ApiId, pricesResult.ErrorMessage);
            return priceByOfferId;
        }

        foreach (var item in pricesResult.Data.Items)
        {
            priceByOfferId[item.OfferId] = item;
        }

        return priceByOfferId;
    }

    private async Task<Dictionary<string, OzonProductAttributeItemDto>> FetchAttributesAsync(
        MarketplaceClient client,
        List<OzonFbsPostingDto> postings,
        CancellationToken ct)
    {
        var offerIds = postings
            .SelectMany(p => p.Products)
            .Select(p => p.OfferId)
            .Distinct()
            .ToList();

        var result = new Dictionary<string, OzonProductAttributeItemDto>();

        if (offerIds.Count == 0)
            return result;

        var lastId = string.Empty;

        do
        {
            var apiResult = await _api.GetProductAttributesAsync(
                client.ApiId, client.Key, offerIds, lastId, ct: ct);

            if (!apiResult.IsSuccess || apiResult.Data?.Result == null)
            {
                _logger.LogWarning(
                    "Failed to fetch product attributes for client {ClientId}: {Error}",
                    client.ApiId, apiResult.ErrorMessage);
                break;
            }

            foreach (var item in apiResult.Data.Result)
                result[item.OfferId] = item;

            lastId = apiResult.Data.LastId ?? string.Empty;
        }
        while (!string.IsNullOrEmpty(lastId));

        return result;
    }

    private async Task UpsertShipmentAsync(
        MarketplaceClient client,
        OzonFbsPostingDto posting,
        Dictionary<string, OzonProductPriceItemDto> priceByOfferId,
        Dictionary<string, OzonProductAttributeItemDto> attributesByOfferId,
        OrderSyncResult stats,
        IDbContextTransaction transaction,
        CancellationToken ct)
    {
        var deliveryMethod = await EnsureDeliveryMethodAsync(posting, ct);
        var warehouseInfo = await EnsureWarehouseInfoAsync(posting, ct);
        var orderStatus = await EnsureOrderStatusAsync(posting.Status, ct);
        var systemBaseStatus = await GetSystemBaseOrderStatusAsync(ct);

        var (shipment, shipmentCreated) = await EnsureShipmentAsync(client, posting, deliveryMethod, orderStatus, ct);
        if (shipmentCreated)
            stats.ShipmentsCreated++;
        else
            stats.ShipmentsUpdated++;

        await EnsureShipmentDatesAsync(shipment, posting, ct);

        var priceType = await EnsurePriceTypeAsync("Цена", "fbs", ct);
        var oldPriceType = await EnsurePriceTypeAsync("Цена до скидки", "fbs", ct);

        foreach (var product in posting.Products)
        {
            var domainProduct = await EnsureProductAsync(product, ct);
            var recipient = await EnsureRecipientAsync(posting.Customer, ct);

            var (order, orderCreated) = await EnsureOrderAsync(shipment, posting.OrderId, product, domainProduct, orderStatus, systemBaseStatus, recipient, warehouseInfo, ct);
            if (orderCreated)
                stats.OrdersCreated++;
            else
                stats.OrdersUpdated++;

            await EnsureProductInfoAsync(order, domainProduct, ct);

            await UpsertOrderPricesAsync(order, product, posting, priceByOfferId, priceType, oldPriceType, ct);

            if (attributesByOfferId.TryGetValue(product.OfferId, out var attrItem))
                await UpsertProductAttributesAsync(domainProduct, attrItem, ct);
        }
    }

    private async Task<DeliveryMethod?> EnsureDeliveryMethodAsync(OzonFbsPostingDto posting, CancellationToken ct)
    {
        if (posting.DeliveryMethod == null)
            return null;

        var deliveryType = await EnsureDeliveryTypeAsync("fbs", ct);

        var dm = await _db.DeliveryMethods
            .FirstOrDefaultAsync(d => d.ExternalId == posting.DeliveryMethod.Id, ct);

        if (dm == null && !string.IsNullOrWhiteSpace(posting.DeliveryMethod.Name))
        {
            // Reuse by name (same delivery type) to avoid duplicates
            dm = await _db.DeliveryMethods
                .FirstOrDefaultAsync(d => d.Name == posting.DeliveryMethod.Name && d.DeliveryTypeId == deliveryType.Id, ct);
            if (dm != null && dm.ExternalId == null)
            {
                dm.ExternalId = posting.DeliveryMethod.Id;
                await _db.SaveChangesAsync(ct);
            }
        }

        if (dm == null)
        {
            dm = new DeliveryMethod
            {
                Id = Guid.NewGuid(),
                ExternalId = posting.DeliveryMethod.Id,
                Name = posting.DeliveryMethod.Name,
                DeliveryTypeId = deliveryType.Id
            };
            _db.DeliveryMethods.Add(dm);
            await _db.SaveChangesAsync(ct);
        }

        return dm;
    }

    private async Task<WarehouseInfo?> EnsureWarehouseInfoAsync(OzonFbsPostingDto posting, CancellationToken ct)
    {
        if (posting.DeliveryMethod == null)
            return null;

        var warehouseName = posting.DeliveryMethod.Warehouse ?? $"Warehouse {posting.DeliveryMethod.WarehouseId}";

        var warehouse = await _db.Warehouses
            .FirstOrDefaultAsync(w => w.ExternalId == posting.DeliveryMethod.WarehouseId, ct);

        if (warehouse == null && !string.IsNullOrWhiteSpace(warehouseName))
        {
            // Reuse warehouse by name to avoid duplicates
            warehouse = await _db.Warehouses
                .FirstOrDefaultAsync(w => w.Name == warehouseName, ct);
            if (warehouse != null && warehouse.ExternalId == null)
            {
                warehouse.ExternalId = posting.DeliveryMethod.WarehouseId;
                await _db.SaveChangesAsync(ct);
            }
        }

        if (warehouse == null)
        {
            warehouse = new Warehouse
            {
                Id = Guid.NewGuid(),
                ExternalId = posting.DeliveryMethod.WarehouseId,
                Name = warehouseName
            };
            _db.Warehouses.Add(warehouse);
            await _db.SaveChangesAsync(ct);
        }

        // Reuse existing WarehouseInfo for the same sender warehouse to avoid duplicates
        var existingWarehouseInfo = await _db.WarehouseInfos
            .FirstOrDefaultAsync(wi => wi.SenderWarehouseId == warehouse.Id, ct);

        if (existingWarehouseInfo != null)
            return existingWarehouseInfo;

        var warehouseInfo = new WarehouseInfo
        {
            Id = Guid.NewGuid(),
            SenderWarehouseId = warehouse.Id
        };
        _db.WarehouseInfos.Add(warehouseInfo);
        await _db.SaveChangesAsync(ct);

        return warehouseInfo;
    }

    private static string GetOzonShipmentStatusDisplayName(string synonym)
    {
        return synonym switch
        {
            "acceptance_in_progress" => "идёт приёмка",
            "arbitration" => "арбитраж",
            "awaiting_approve" => "ожидает подтверждения",
            "awaiting_deliver" => "ожидает отгрузки",
            "awaiting_packaging" => "ожидает упаковки",
            "awaiting_registration" => "ожидает регистрации",
            "awaiting_verification" => "создано",
            "cancelled" => "отменено",
            "cancelled_from_split_pending" => "отменён из-за разделения отправления",
            "client_arbitration" => "клиентский арбитраж доставки",
            "delivering" => "доставляется",
            "driver_pickup" => "у водителя",
            "not_accepted" => "не принят на сортировочном центре",
            _ => synonym
        };
    }

    private async Task<OrderStatus> EnsureOrderStatusAsync(string synonym, CancellationToken ct)
    {
        var normalized = synonym?.Trim() ?? "";
        var status = await _db.OrderStatuses
            .FirstOrDefaultAsync(s => s.Synonym != null && s.Synonym.ToLower() == normalized.ToLower(), ct);

        if (status != null)
            return status;

        var ozonType = await _db.MarketplaceClientTypes!
            .FirstOrDefaultAsync(t => t.Name == "Ozon", ct);

        var displayName = GetOzonShipmentStatusDisplayName(normalized);
        status = new OrderStatus
        {
            Id = Guid.NewGuid(),
            Name = displayName,
            Synonym = normalized,
            IsInternal = false,
            MarketplaceClientTypeId = ozonType?.Id
        };
        _db.OrderStatuses.Add(status);
        await _db.SaveChangesAsync(ct);

        return status;
    }

    private async Task<OrderStatus?> GetSystemBaseOrderStatusAsync(CancellationToken ct)
    {
        return await _db.OrderStatuses
            .FirstOrDefaultAsync(s => s.Name == "Не указан" && s.IsInternal, ct);
    }

    private async Task<(Shipment Shipment, bool Created)> EnsureShipmentAsync(
        MarketplaceClient client,
        OzonFbsPostingDto posting,
        DeliveryMethod? deliveryMethod,
        OrderStatus status,
        CancellationToken ct)
    {
        var shipment = await _db.Shipments
            .FirstOrDefaultAsync(s => s.PostingNumber == posting.PostingNumber, ct);

        var created = false;
        if (shipment == null)
        {
            created = true;
            shipment = new Shipment
            {
                Id = Guid.NewGuid(),
                PostingNumber = posting.PostingNumber,
                MarketplaceClientId = client.Id,
                CreatedAt = DateTime.UtcNow
            };
            _db.Shipments.Add(shipment);
        }

        shipment.OrderNumber = posting.OrderNumber;
        shipment.StatusId = status.Id;
        shipment.DeliveryMethodId = deliveryMethod?.Id;
        shipment.TrackingNumber = posting.TrackingNumber;
        shipment.InProcessAt = posting.InProcessAt;
        shipment.ShipmentDate = posting.ShipmentDate;

        await _db.SaveChangesAsync(ct);

        return (shipment, created);
    }

    private async Task EnsureShipmentDatesAsync(
        Shipment shipment,
        OzonFbsPostingDto posting,
        CancellationToken ct)
    {
        // Дата отгрузки (shipment_date)
        if (posting.ShipmentDate.HasValue)
        {
            var shipmentDateType = await EnsureDateTypeAsync("Дата отгрузки", ct);
            await UpsertShipmentDateAsync(shipment.Id, shipmentDateType.Id, posting.ShipmentDate.Value, ct);
        }

        // Дата начала обработки (in_process_at)
        if (posting.InProcessAt.HasValue)
        {
            var inProcessType = await EnsureDateTypeAsync("Дата начала обработки", ct);
            await UpsertShipmentDateAsync(shipment.Id, inProcessType.Id, posting.InProcessAt.Value, ct);
        }
    }

    private async Task<Product> EnsureProductAsync(OzonFbsProductDto product, CancellationToken ct)
    {
        var domainProduct = await _db.Products
            .FirstOrDefaultAsync(p => p.Sku == product.Sku, ct);

        if (domainProduct == null && !string.IsNullOrWhiteSpace(product.OfferId))
        {
            // Reuse by article (offer_id) to avoid duplicates
            domainProduct = await _db.Products
                .FirstOrDefaultAsync(p => p.Article == product.OfferId, ct);
        }

        if (domainProduct == null)
        {
            domainProduct = new Product
            {
                Id = Guid.NewGuid(),
                Sku = product.Sku,
                Article = product.OfferId,
                Name = product.Name
            };
            _db.Products.Add(domainProduct);
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            if (domainProduct.Sku == null || domainProduct.Sku != product.Sku)
                domainProduct.Sku = product.Sku;
            domainProduct.Article ??= product.OfferId;
            if (string.IsNullOrEmpty(domainProduct.Name))
                domainProduct.Name = product.Name;
            await _db.SaveChangesAsync(ct);
        }

        return domainProduct;
    }

    private async Task<Recipient?> EnsureRecipientAsync(OzonFbsCustomerDto? customer, CancellationToken ct)
    {
        if (customer == null)
            return null;

        // Reuse existing recipient by phone or name to avoid duplicates
        if (!string.IsNullOrWhiteSpace(customer.Phone))
        {
            var existingByPhone = await _db.Recipients
                .FirstOrDefaultAsync(r => r.Phone == customer.Phone, ct);
            if (existingByPhone != null)
                return existingByPhone;
        }

        if (!string.IsNullOrWhiteSpace(customer.Name))
        {
            var existingByName = await _db.Recipients
                .FirstOrDefaultAsync(r => r.Name == customer.Name, ct);
            if (existingByName != null)
                return existingByName;
        }

        Address? address = null;
        if (customer.Address != null)
        {
            address = new Address
            {
                Id = Guid.NewGuid(),
                City = customer.Address.City,
                Region = customer.Address.Region,
                PostalCode = customer.Address.ZipCode,
                Street = customer.Address.AddressTail
            };
            _db.Addresses.Add(address);
            await _db.SaveChangesAsync(ct);
        }

        var recipient = new Recipient
        {
            Id = Guid.NewGuid(),
            Name = customer.Name,
            Phone = customer.Phone,
            AddressId = address?.Id
        };
        _db.Recipients.Add(recipient);
        await _db.SaveChangesAsync(ct);

        return recipient;
    }

    private async Task<(Order Order, bool Created)> EnsureOrderAsync(
        Shipment shipment,
        long ozonOrderId,
        OzonFbsProductDto product,
        Product domainProduct,
        OrderStatus status,
        OrderStatus? systemBaseStatus,
        Recipient? recipient,
        WarehouseInfo? warehouseInfo,
        CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.ProductInfo)
            .FirstOrDefaultAsync(o =>
                o.ShipmentId == shipment.Id
                && o.ProductInfo != null
                && o.ProductInfo.ProductId == domainProduct.Id, ct);

        var created = false;
        if (order == null)
        {
            created = true;
            var orderProductInfo = new OrderProductInfo
            {
                Id = Guid.NewGuid(),
                ProductId = domainProduct.Id
            };
            order = new Order
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipment.Id,
                OzonOrderId = ozonOrderId,
                ProductInfoId = orderProductInfo.Id,
                SystemStatusId = systemBaseStatus?.Id
            };
            orderProductInfo.OrderId = order.Id;
            _db.Orders.Add(order);
            _db.OrderProductInfos.Add(orderProductInfo);
        }

        order.Quantity = product.Quantity;
        order.StatusId = status.Id;
        order.RecipientId = recipient?.Id;
        order.WarehouseInfoId = warehouseInfo?.Id;
        order.OzonOrderId = ozonOrderId;

        await _db.SaveChangesAsync(ct);

        return (order, created);
    }

    private async Task EnsureProductInfoAsync(Order order, Product product, CancellationToken ct)
    {
        var exists = await _db.OrderProductInfos
            .AnyAsync(pi => pi.OrderId == order.Id, ct);

        if (!exists)
        {
            _db.OrderProductInfos.Add(new OrderProductInfo
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = product.Id
            });
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task UpsertShipmentDateAsync(
        Guid shipmentId,
        Guid dateTypeId,
        DateTime value,
        CancellationToken ct)
    {
        var existing = await _db.ShipmentDates
            .FirstOrDefaultAsync(d => d.ShipmentId == shipmentId && d.DateTypeId == dateTypeId, ct);

        if (existing == null)
        {
            _db.ShipmentDates.Add(new ShipmentDate
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentId,
                DateTypeId = dateTypeId,
                Value = value
            });
        }
        else
        {
            existing.Value = value;
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task UpsertOrderPricesAsync(
        Order order,
        OzonFbsProductDto product,
        OzonFbsPostingDto posting,
        Dictionary<string, OzonProductPriceItemDto> priceByOfferId,
        PriceType? priceType,
        PriceType? oldPriceType,
        CancellationToken ct)
    {
        priceByOfferId.TryGetValue(product.OfferId, out var priceItem);
        var priceDto = priceItem?.Price;

        var currencyCode = product.CurrencyCode ?? priceDto?.CurrencyCode;

        Currency? currency = null;
        if (!string.IsNullOrEmpty(currencyCode))
            currency = await EnsureCurrencyAsync(currencyCode, ct);

        // Базовая цена из постинга (product.price)
        if (priceType != null && !string.IsNullOrEmpty(product.Price)
            && decimal.TryParse(product.Price, NumberStyles.Any, CultureInfo.InvariantCulture, out var priceValue))
        {
            await UpsertSinglePriceAsync(order.Id, priceType.Id, currency?.Id, priceValue, ct);
        }

        if (priceDto != null)
        {
            // Цена до скидки
            if (oldPriceType != null && priceDto.OldPrice.HasValue && priceDto.OldPrice.Value > 0)
                await UpsertSinglePriceAsync(order.Id, oldPriceType.Id, currency?.Id, priceDto.OldPrice.Value, ct);

            // Минимальная цена
            if (priceDto.MinPrice.HasValue && priceDto.MinPrice.Value > 0)
            {
                var t = await EnsurePriceTypeAsync("Минимальная цена", "fbs", ct);
                await UpsertSinglePriceAsync(order.Id, t.Id, currency?.Id, priceDto.MinPrice.Value, ct);
            }

            // Себестоимость
            if (priceDto.NetPrice.HasValue && priceDto.NetPrice.Value > 0)
            {
                var t = await EnsurePriceTypeAsync("Себестоимость", "fbs", ct);
                await UpsertSinglePriceAsync(order.Id, t.Id, currency?.Id, priceDto.NetPrice.Value, ct);
            }

            // Маркетинговая цена продавца
            if (priceDto.MarketingSellerPrice.HasValue && priceDto.MarketingSellerPrice.Value > 0)
            {
                var t = await EnsurePriceTypeAsync("Маркетинговая цена продавца", "fbs", ct);
                await UpsertSinglePriceAsync(order.Id, t.Id, currency?.Id, priceDto.MarketingSellerPrice.Value, ct);
            }

            // Розничная цена поставщика
            if (priceDto.RetailPrice.HasValue && priceDto.RetailPrice.Value > 0)
            {
                var t = await EnsurePriceTypeAsync("Розничная цена поставщика", "fbs", ct);
                await UpsertSinglePriceAsync(order.Id, t.Id, currency?.Id, priceDto.RetailPrice.Value, ct);
            }
        }

        // Резервный источник старой цены — financial_data из постинга
        if (oldPriceType != null)
        {
            var oldPriceValue = GetOldPrice(product, posting, priceByOfferId);
            if (oldPriceValue.HasValue && oldPriceValue.Value > 0)
                await UpsertSinglePriceAsync(order.Id, oldPriceType.Id, currency?.Id, oldPriceValue.Value, ct);
        }

        // Комиссии FBS из /v5/product/info/prices
        if (priceItem?.Commissions is { } c)
        {
            await UpsertCommissionPriceAsync(order.Id, currency?.Id, "Доставка до покупателя (FBS)", c.FbsDelivToCustomerAmount, ct);
            await UpsertCommissionPriceAsync(order.Id, currency?.Id, "Транзит макс. (FBS)", c.FbsDirectFlowTransMaxAmount, ct);
            await UpsertCommissionPriceAsync(order.Id, currency?.Id, "Транзит мин. (FBS)", c.FbsDirectFlowTransMinAmount, ct);
            await UpsertCommissionPriceAsync(order.Id, currency?.Id, "Первая миля макс. (FBS)", c.FbsFirstMileMaxAmount, ct);
            await UpsertCommissionPriceAsync(order.Id, currency?.Id, "Первая миля мин. (FBS)", c.FbsFirstMileMinAmount, ct);
            await UpsertCommissionPriceAsync(order.Id, currency?.Id, "Возврат (FBS)", c.FbsReturnFlowAmount, ct);
            await UpsertCommissionPriceAsync(order.Id, currency?.Id, "Доставка до покупателя (FBO)", c.FboDelivToCustomerAmount, ct);
            await UpsertCommissionPriceAsync(order.Id, currency?.Id, "Транзит макс. (FBO)", c.FboDirectFlowTransMaxAmount, ct);
            await UpsertCommissionPriceAsync(order.Id, currency?.Id, "Транзит мин. (FBO)", c.FboDirectFlowTransMinAmount, ct);
            await UpsertCommissionPriceAsync(order.Id, currency?.Id, "Возврат (FBO)", c.FboReturnFlowAmount, ct);

            if (c.SalesPercentFbs.HasValue && c.SalesPercentFbs.Value > 0)
            {
                var t = await EnsurePriceTypeAsync("% продаж FBS", "fbs", ct);
                await UpsertSinglePriceAsync(order.Id, t.Id, null, (decimal)c.SalesPercentFbs.Value, ct);
            }
            if (c.SalesPercentFbo.HasValue && c.SalesPercentFbo.Value > 0)
            {
                var t = await EnsurePriceTypeAsync("% продаж FBO", "fbs", ct);
                await UpsertSinglePriceAsync(order.Id, t.Id, null, (decimal)c.SalesPercentFbo.Value, ct);
            }
        }

        // Индексы цены из /v5/product/info/prices
        if (priceItem?.PriceIndexes is { } idx)
        {
            if (idx.OzonIndexData?.MinPrice is { } ozonMin && ozonMin > 0)
            {
                var ozonCurrency = string.IsNullOrEmpty(idx.OzonIndexData.MinPriceCurrency)
                    ? currency
                    : await EnsureCurrencyAsync(idx.OzonIndexData.MinPriceCurrency, ct);
                var t = await EnsurePriceTypeAsync("Мин. цена Ozon (индекс)", "fbs", ct);
                await UpsertSinglePriceAsync(order.Id, t.Id, ozonCurrency?.Id, ozonMin, ct);
            }
            if (idx.ExternalIndexData?.MinPrice is { } extMin && extMin > 0)
            {
                var extCurrency = string.IsNullOrEmpty(idx.ExternalIndexData.MinPriceCurrency)
                    ? currency
                    : await EnsureCurrencyAsync(idx.ExternalIndexData.MinPriceCurrency, ct);
                var t = await EnsurePriceTypeAsync("Мин. цена внешних МП (индекс)", "fbs", ct);
                await UpsertSinglePriceAsync(order.Id, t.Id, extCurrency?.Id, extMin, ct);
            }
            if (idx.SelfMarketplacesIndexData?.MinPrice is { } selfMin && selfMin > 0)
            {
                var selfCurrency = string.IsNullOrEmpty(idx.SelfMarketplacesIndexData.MinPriceCurrency)
                    ? currency
                    : await EnsureCurrencyAsync(idx.SelfMarketplacesIndexData.MinPriceCurrency, ct);
                var t = await EnsurePriceTypeAsync("Мин. цена в соб. магазинах (индекс)", "fbs", ct);
                await UpsertSinglePriceAsync(order.Id, t.Id, selfCurrency?.Id, selfMin, ct);
            }
        }
    }

    private async Task UpsertCommissionPriceAsync(
        Guid orderId,
        Guid? currencyId,
        string name,
        decimal? amount,
        CancellationToken ct)
    {
        if (!amount.HasValue || amount.Value <= 0)
            return;

        var t = await EnsurePriceTypeAsync(name, "fbs", ct);
        await UpsertSinglePriceAsync(orderId, t.Id, currencyId, amount.Value, ct);
    }

    private async Task UpsertSinglePriceAsync(
        Guid orderId,
        Guid priceTypeId,
        Guid? currencyId,
        decimal value,
        CancellationToken ct)
    {
        var existing = await _db.OrderPrices
            .FirstOrDefaultAsync(p => p.OrderId == orderId && p.PriceTypeId == priceTypeId, ct);

        if (existing == null)
        {
            _db.OrderPrices.Add(new OrderPrice
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                PriceTypeId = priceTypeId,
                CurrencyId = currencyId,
                Value = value
            });
        }
        else
        {
            existing.Value = value;
            existing.CurrencyId = currencyId;
        }

        await _db.SaveChangesAsync(ct);
    }

    private static decimal? GetOldPrice(
        OzonFbsProductDto product,
        OzonFbsPostingDto posting,
        Dictionary<string, OzonProductPriceItemDto> priceByOfferId)
    {
        var financialProduct = posting.FinancialData?.Products
            .FirstOrDefault(fp => fp.ProductId == product.Sku);

        if (financialProduct != null && financialProduct.OldPrice > 0)
            return financialProduct.OldPrice;

        if (priceByOfferId.TryGetValue(product.OfferId, out var priceItem)
            && priceItem.Price?.OldPrice is { } oldPrice
            && oldPrice > 0)
        {
            return oldPrice;
        }

        return null;
    }

    private async Task UpsertProductAttributesAsync(
        Product product,
        OzonProductAttributeItemDto item,
        CancellationToken ct)
    {
        // Обновляем базовые поля товара из ответа /v4/product/info/attributes
        var changed = false;

        if (!string.IsNullOrEmpty(item.PrimaryImage) && string.IsNullOrEmpty(product.ImageUrl))
        {
            product.ImageUrl = item.PrimaryImage;
            changed = true;
        }

        if (!string.IsNullOrEmpty(item.Barcode) && string.IsNullOrEmpty(product.Barcode))
        {
            product.Barcode = item.Barcode;
            changed = true;
        }
        else if (product.Barcode == null && item.Barcodes?.Count > 0)
        {
            product.Barcode = item.Barcodes[0];
            changed = true;
        }

        if (changed)
            await _db.SaveChangesAsync(ct);

        // Сохраняем все характеристики (attributes + complex_attributes)
        var allAttributes = item.Attributes.Concat(item.ComplexAttributes);

        foreach (var attr in allAttributes)
        {
            if (attr.Values.Count == 0)
                continue;

            var attrCode = attr.ComplexId > 0
                ? $"{attr.ComplexId}_{attr.Id}"
                : attr.Id.ToString();

            var domainAttr = await _db.ProductAttributes
                .FirstOrDefaultAsync(a => a.Code == attrCode, ct);

            if (domainAttr == null)
            {
                domainAttr = new ProductAttribute
                {
                    Id = Guid.NewGuid(),
                    Code = attrCode,
                    Name = attrCode
                };
                _db.ProductAttributes.Add(domainAttr);
                await _db.SaveChangesAsync(ct);
            }

            // Объединяем значения через разделитель, если их несколько
            var combinedValue = string.Join("; ", attr.Values
                .Where(v => !string.IsNullOrWhiteSpace(v.Value))
                .Select(v => v.Value!));

            if (string.IsNullOrEmpty(combinedValue))
                continue;

            var existing = await _db.ProductAttributeValues
                .FirstOrDefaultAsync(
                    v => v.ProductId == product.Id && v.AttributeId == domainAttr.Id, ct);

            if (existing == null)
            {
                _db.ProductAttributeValues.Add(new ProductAttributeValue
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    AttributeId = domainAttr.Id,
                    Value = combinedValue
                });
            }
            else
            {
                existing.Value = combinedValue;
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<DeliveryType> EnsureDeliveryTypeAsync(string code, CancellationToken ct)
    {
        var existing = await _db.DeliveryTypes
            .FirstOrDefaultAsync(t => t.Name == code, ct);

        if (existing != null)
            return existing;

        var type = new DeliveryType
        {
            Id = Guid.NewGuid(),
            Name = code
        };
        _db.DeliveryTypes.Add(type);
        await _db.SaveChangesAsync(ct);
        return type;
    }

    private async Task<DateType> EnsureDateTypeAsync(string name, CancellationToken ct)
    {
        var existing = await _db.DateTypes
            .FirstOrDefaultAsync(t => t.Name == name, ct);

        if (existing != null)
            return existing;

        var type = new DateType
        {
            Id = Guid.NewGuid(),
            Name = name
        };
        _db.DateTypes.Add(type);
        await _db.SaveChangesAsync(ct);
        return type;
    }

    private async Task<Currency?> EnsureCurrencyAsync(string code, CancellationToken ct)
    {
        var existing = await _db.Currencies.FirstOrDefaultAsync(c => c.Code == code, ct);
        if (existing != null)
            return existing;

        var currency = new Currency
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = code
        };
        _db.Currencies.Add(currency);
        await _db.SaveChangesAsync(ct);
        return currency;
    }

    private async Task<PriceType> EnsurePriceTypeAsync(string name, string deliveryScheme, CancellationToken ct)
    {
        var existing = await _db.PriceTypes
            .FirstOrDefaultAsync(pt => pt.Name == name && pt.DeliveryScheme == deliveryScheme, ct);

        if (existing != null)
            return existing;

        var priceType = new PriceType
        {
            Id = Guid.NewGuid(),
            Name = name,
            DeliveryScheme = deliveryScheme
        };
        _db.PriceTypes.Add(priceType);
        await _db.SaveChangesAsync(ct);
        return priceType;
    }
}
