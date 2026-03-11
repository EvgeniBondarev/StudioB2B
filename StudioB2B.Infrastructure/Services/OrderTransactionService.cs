using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioB2B.Domain.Constants;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Features.Orders;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Services;

public class OrderTransactionService : IOrderTransactionService
{
    private readonly TenantDbContext _db;
    private readonly CalculationEngine _calcEngine;
    private readonly ICurrentUserProvider _currentUser;
    private readonly ILogger<OrderTransactionService> _logger;

    public OrderTransactionService(
        TenantDbContext db,
        CalculationEngine calcEngine,
        ICurrentUserProvider currentUser,
        ILogger<OrderTransactionService> logger)
    {
        _db = db;
        _calcEngine = calcEngine;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<TransactionApplyPreview?> GetApplyPreviewAsync(Guid orderId, Guid transactionId, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .IncludeForGrid()
            .Include(o => o.Prices).ThenInclude(p => p.PriceType)
            .Include(o => o.ProductInfo).ThenInclude(pi => pi!.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null) return null;

        var transaction = await _db.OrderTransactions
            .Include(t => t.ToSystemStatus)
            .Include(t => t.Rules.OrderBy(r => r.SortOrder))
                .ThenInclude(r => r.PriceType)
            .Include(t => t.Rules).ThenInclude(r => r.Product)
            .Include(t => t.FieldRules)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == transactionId && !t.IsDeleted, ct);

        if (transaction == null || !transaction.IsEnabled || order.SystemStatusId != transaction.FromSystemStatusId)
            return null;

        var context = await BuildContextAsync(order, ct);
        var rules = new List<TransactionApplyRulePreview>();

        foreach (var rule in transaction.Rules.OrderBy(r => r.SortOrder))
        {
            if (rule.PriceType == null) continue;
            if (rule.ProductId.HasValue && order.ProductInfo?.ProductId != rule.ProductId.Value)
                continue;

            decimal? computed = null;
            string? breakdown = null;
            if (rule.ValueSource == TransactionValueSourceEnum.Formula && !string.IsNullOrWhiteSpace(rule.Formula))
            {
                try
                {
                    computed = _calcEngine.EvaluateFormula(rule.Formula, context);
                    breakdown = BuildFormulaBreakdown(rule.Formula, context, computed.Value);
                }
                catch { /* ignore */ }
            }

            rules.Add(new TransactionApplyRulePreview
            {
                RuleId = rule.Id,
                PriceTypeId = rule.PriceTypeId,
                PriceTypeName = rule.PriceType.Name ?? "",
                ProductId = rule.ProductId,
                ProductName = rule.Product?.Name,
                ValueSource = rule.ValueSource,
                Formula = rule.Formula,
                ComputedValue = computed,
                FormulaBreakdown = breakdown,
                IsRequired = rule.IsRequired
            });
        }

        var fieldRules = new List<TransactionApplyFieldRulePreview>();
        foreach (var fr in transaction.FieldRules.OrderBy(r => r.SortOrder))
        {
            var descriptor = OrderTransactionFieldRegistry.Get(fr.EntityPath);
            if (descriptor == null) continue;

            fieldRules.Add(new TransactionApplyFieldRulePreview
            {
                RuleId = fr.Id,
                EntityPath = fr.EntityPath,
                DisplayName = descriptor.DisplayName,
                ValueSource = fr.ValueSource,
                FixedValue = fr.FixedValue,
                ValueType = descriptor.ValueType,
                ReferenceType = descriptor.ReferenceType,
                IsRequired = fr.IsRequired
            });
        }

        return new TransactionApplyPreview
        {
            TransactionName = transaction.Name,
            ToStatusName = transaction.ToSystemStatus?.Name ?? "",
            Rules = rules,
            FieldRules = fieldRules
        };
    }

    public async Task<TransactionApplyPreview?> GetApplyPreviewWithUserValuesAsync(Guid orderId, Guid transactionId, IReadOnlyDictionary<Guid, decimal>? userValues, CancellationToken ct = default)
    {
        var context = await GetMergedContextAsync(orderId, transactionId, userValues ?? new Dictionary<Guid, decimal>(), ct);
        if (context == null) return null;

        var order = await _db.Orders
            .IncludeForGrid()
            .Include(o => o.Prices).ThenInclude(p => p.PriceType)
            .Include(o => o.ProductInfo).ThenInclude(pi => pi!.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null) return null;

        var transaction = await _db.OrderTransactions
            .Include(t => t.ToSystemStatus)
            .Include(t => t.Rules.OrderBy(r => r.SortOrder))
                .ThenInclude(r => r.PriceType)
            .Include(t => t.Rules).ThenInclude(r => r.Product)
            .Include(t => t.FieldRules)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == transactionId && !t.IsDeleted, ct);

        if (transaction == null || !transaction.IsEnabled || order.SystemStatusId != transaction.FromSystemStatusId)
            return null;

        var ctx = new Dictionary<string, decimal>(context, StringComparer.OrdinalIgnoreCase);
        var rules = new List<TransactionApplyRulePreview>();

        foreach (var rule in transaction.Rules.OrderBy(r => r.SortOrder))
        {
            if (rule.PriceType == null) continue;
            if (rule.ProductId.HasValue && order.ProductInfo?.ProductId != rule.ProductId.Value)
                continue;

            var priceKey = CalculationEngine.SanitizeKey(rule.PriceType.Name ?? string.Empty);

            if (rule.ValueSource == TransactionValueSourceEnum.UserInput)
            {
                rules.Add(new TransactionApplyRulePreview
                {
                    RuleId = rule.Id,
                    PriceTypeId = rule.PriceTypeId,
                    PriceTypeName = rule.PriceType.Name ?? "",
                    ProductId = rule.ProductId,
                    ProductName = rule.Product?.Name,
                    ValueSource = rule.ValueSource,
                    IsRequired = rule.IsRequired
                });
            }
            else if (rule.ValueSource == TransactionValueSourceEnum.Formula && !string.IsNullOrWhiteSpace(rule.Formula))
            {
                decimal? computed = null;
                string? breakdown = null;
                try
                {
                    computed = _calcEngine.EvaluateFormula(rule.Formula, ctx);
                    if (computed.HasValue)
                    {
                        breakdown = BuildFormulaBreakdown(rule.Formula, ctx, computed.Value);
                        if (!string.IsNullOrEmpty(priceKey))
                            ctx[priceKey] = computed.Value;
                    }
                }
                catch { /* ignore */ }

                rules.Add(new TransactionApplyRulePreview
                {
                    RuleId = rule.Id,
                    PriceTypeId = rule.PriceTypeId,
                    PriceTypeName = rule.PriceType.Name ?? "",
                    ProductId = rule.ProductId,
                    ProductName = rule.Product?.Name,
                    ValueSource = rule.ValueSource,
                    Formula = rule.Formula,
                    ComputedValue = computed,
                    FormulaBreakdown = breakdown,
                    IsRequired = rule.IsRequired
                });
            }
        }

        var fieldRules = new List<TransactionApplyFieldRulePreview>();
        foreach (var fr in transaction.FieldRules.OrderBy(r => r.SortOrder))
        {
            var descriptor = OrderTransactionFieldRegistry.Get(fr.EntityPath);
            if (descriptor == null) continue;

            fieldRules.Add(new TransactionApplyFieldRulePreview
            {
                RuleId = fr.Id,
                EntityPath = fr.EntityPath,
                DisplayName = descriptor.DisplayName,
                ValueSource = fr.ValueSource,
                FixedValue = fr.FixedValue,
                ValueType = descriptor.ValueType,
                ReferenceType = descriptor.ReferenceType,
                IsRequired = fr.IsRequired
            });
        }

        return new TransactionApplyPreview
        {
            TransactionName = transaction.Name,
            ToStatusName = transaction.ToSystemStatus?.Name ?? "",
            Rules = rules,
            FieldRules = fieldRules
        };
    }

    public async Task<IReadOnlyDictionary<string, decimal>?> GetMergedContextAsync(Guid orderId, Guid transactionId, IReadOnlyDictionary<Guid, decimal> userValues, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .IncludeForGrid()
            .Include(o => o.Prices).ThenInclude(p => p.PriceType)
            .Include(o => o.ProductInfo).ThenInclude(pi => pi!.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null) return null;

        var transaction = await _db.OrderTransactions
            .Include(t => t.Rules.OrderBy(r => r.SortOrder))
                .ThenInclude(r => r.PriceType)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == transactionId && !t.IsDeleted, ct);

        if (transaction == null || !transaction.IsEnabled || order.SystemStatusId != transaction.FromSystemStatusId)
            return null;

        var context = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["Quantity"] = order.Quantity
        };

        foreach (var price in order.Prices)
        {
            if (price.PriceType == null) continue;
            var key = CalculationEngine.SanitizeKey(price.PriceType.Name ?? string.Empty);
            if (!string.IsNullOrEmpty(key))
                context[key] = price.Value;
        }

        foreach (var rule in transaction.Rules.OrderBy(r => r.SortOrder))
        {
            if (rule.PriceType == null) continue;
            if (rule.ProductId.HasValue && order.ProductInfo?.ProductId != rule.ProductId.Value)
                continue;
            if (rule.ValueSource != TransactionValueSourceEnum.UserInput) continue;
            if (!userValues.TryGetValue(rule.Id, out var uv)) continue;

            var key = CalculationEngine.SanitizeKey(rule.PriceType.Name ?? string.Empty);
            if (!string.IsNullOrEmpty(key))
                context[key] = uv;
        }

        var calcRules = await _db.CalculationRules
            .Where(r => !r.IsDeleted && r.IsActive)
            .OrderBy(r => r.SortOrder)
            .AsNoTracking()
            .ToListAsync(ct);

        if (calcRules.Count > 0)
        {
            var computed = _calcEngine.CalculateWithContext(context, calcRules);
            foreach (var kv in computed.Where(k => k.Value != decimal.MinValue))
            {
                var key = CalculationEngine.SanitizeKey(kv.Key);
                if (!string.IsNullOrEmpty(key))
                    context[key] = kv.Value;
            }
        }

        return context;
    }

    public async Task<TransactionApplyResult> ApplyAsync(Guid orderId, Guid transactionId, IReadOnlyDictionary<Guid, decimal>? ruleValues = null, IReadOnlyDictionary<Guid, string>? fieldRuleValues = null, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .IncludeForGrid()
            .Include(o => o.Recipient)
            .Include(o => o.Prices).ThenInclude(p => p.PriceType)
            .Include(o => o.Prices).ThenInclude(p => p.Currency)
            .Include(o => o.ProductInfo).ThenInclude(pi => pi!.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null)
            return new TransactionApplyResult { Success = false, ErrorMessage = "Заказ не найден" };

        var transaction = await _db.OrderTransactions
            .Include(t => t.FromSystemStatus)
            .Include(t => t.ToSystemStatus)
            .Include(t => t.Rules.OrderBy(r => r.SortOrder))
                .ThenInclude(r => r.PriceType)
            .Include(t => t.Rules).ThenInclude(r => r.Currency)
            .Include(t => t.Rules).ThenInclude(r => r.Product)
            .Include(t => t.FieldRules)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == transactionId && !t.IsDeleted, ct);

        if (transaction == null)
            return new TransactionApplyResult { Success = false, ErrorMessage = "Транзакция не найдена" };

        if (!transaction.IsEnabled)
        {
            await AddHistoryAsync(orderId, transactionId, false, "Транзакция отключена", 0, 0, ct);
            await _db.SaveChangesAsync(ct);
            return new TransactionApplyResult { Success = false, ErrorMessage = "Транзакция отключена" };
        }

        if (order.SystemStatusId != transaction.FromSystemStatusId)
        {
            var msg = $"Текущий статус заказа ({order.SystemStatus?.Name ?? "—"}) не совпадает с исходным статусом транзакции ({transaction.FromSystemStatus?.Name ?? "—"})";
            await AddHistoryAsync(orderId, transactionId, false, msg, 0, 0, ct);
            await _db.SaveChangesAsync(ct);
            return new TransactionApplyResult { Success = false, ErrorMessage = msg };
        }

        var priceErrors = ValidateRequiredPriceRules(order, transaction.Rules, ruleValues ?? new Dictionary<Guid, decimal>());
        var fieldErrors = ValidateRequiredFieldRules(transaction.FieldRules, fieldRuleValues ?? new Dictionary<Guid, string>());
        var validationErrors = priceErrors.Concat(fieldErrors).ToList();
        if (validationErrors.Count > 0)
        {
            var msg = "Не заполнены обязательные поля";
            await AddHistoryAsync(orderId, transactionId, false, msg, 0, 0, ct);
            await _db.SaveChangesAsync(ct);
            return new TransactionApplyResult { Success = false, ErrorMessage = msg, ValidationErrors = validationErrors };
        }

        var context = await BuildContextAsync(order, ct);
        var pricesUpdated = 0;

        foreach (var rule in transaction.Rules.OrderBy(r => r.SortOrder))
        {
            if (rule.PriceType == null)
                continue;

            if (rule.ProductId.HasValue && order.ProductInfo?.ProductId != rule.ProductId.Value)
                continue;

            decimal value;
            if (rule.ValueSource == TransactionValueSourceEnum.UserInput)
            {
                if (ruleValues == null || !ruleValues.TryGetValue(rule.Id, out value))
                    continue;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(rule.Formula))
                    continue;
                try
                {
                    value = _calcEngine.EvaluateFormula(rule.Formula, context);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ошибка вычисления формулы правила {RuleId}: {Formula}", rule.Id, rule.Formula);
                    continue;
                }
            }

            var currencyId = rule.CurrencyId ?? order.Prices.FirstOrDefault()?.CurrencyId;
            var updated = await UpsertOrderPriceAsync(orderId, rule.PriceTypeId, currencyId, value, ct);
            if (updated) pricesUpdated++;

            context[CalculationEngine.SanitizeKey(rule.PriceType.Name)] = value;
        }

        var fieldsUpdated = await ApplyFieldRulesAsync(order, transaction.FieldRules.OrderBy(r => r.SortOrder), fieldRuleValues ?? new Dictionary<Guid, string>(), ct);

        order.SystemStatusId = transaction.ToSystemStatusId;
        await AddHistoryAsync(orderId, transactionId, true, null, pricesUpdated, fieldsUpdated, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Transaction {TransactionName} applied to order {OrderId}: {PricesUpdated} prices, {FieldsUpdated} fields, status -> {ToStatus}",
            transaction.Name, orderId, pricesUpdated, fieldsUpdated, transaction.ToSystemStatus?.Name);

        return new TransactionApplyResult
        {
            Success = true,
            PricesUpdated = pricesUpdated,
            FieldsUpdated = fieldsUpdated
        };
    }

    private Task AddHistoryAsync(
        Guid orderId,
        Guid transactionId,
        bool success,
        string? errorMessage,
        int pricesUpdated,
        int fieldsUpdated,
        CancellationToken ct)
    {
        var userName = _currentUser.IsAuthenticated
            ? (_currentUser.Email ?? "—")
            : SystemUser.RobotEmail;

        _db.OrderTransactionHistories.Add(new OrderTransactionHistory
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            OrderTransactionId = transactionId,
            PerformedAtUtc = DateTime.UtcNow,
            PerformedByUserId = _currentUser.UserId,
            PerformedByUserName = userName,
            Success = success,
            ErrorMessage = errorMessage,
            PricesUpdated = pricesUpdated,
            FieldsUpdated = fieldsUpdated
        });
        return Task.CompletedTask;
    }

    private static List<string> ValidateRequiredPriceRules(
        Order order,
        IEnumerable<OrderTransactionRule> rules,
        IReadOnlyDictionary<Guid, decimal> ruleValues)
    {
        var errors = new List<string>();
        foreach (var rule in rules)
        {
            if (rule.ValueSource != TransactionValueSourceEnum.UserInput || !rule.IsRequired)
                continue;
            if (rule.PriceType == null) continue;
            if (rule.ProductId.HasValue && order.ProductInfo?.ProductId != rule.ProductId.Value)
                continue;

            if (!ruleValues.TryGetValue(rule.Id, out _))
                errors.Add(rule.PriceType.Name ?? "Цена");
        }
        return errors;
    }

    private static List<string> ValidateRequiredFieldRules(
        IEnumerable<OrderTransactionFieldRule> fieldRules,
        IReadOnlyDictionary<Guid, string> fieldRuleValues)
    {
        var errors = new List<string>();
        foreach (var rule in fieldRules)
        {
            if (rule.ValueSource != TransactionFieldValueSourceEnum.UserInput || !rule.IsRequired)
                continue;

            var descriptor = OrderTransactionFieldRegistry.Get(rule.EntityPath);
            if (descriptor == null) continue;

            if (!fieldRuleValues.TryGetValue(rule.Id, out var valueStr))
                valueStr = null;

            if (IsFieldValueEmpty(valueStr, descriptor.ValueType))
                errors.Add(descriptor.DisplayName);
        }
        return errors;
    }

    private static bool IsFieldValueEmpty(string? valueStr, TransactionFieldValueTypeEnum valueType)
    {
        if (string.IsNullOrWhiteSpace(valueStr)) return true;
        return valueType switch
        {
            TransactionFieldValueTypeEnum.Guid => !Guid.TryParse(valueStr.Trim(), out var g) || g == Guid.Empty,
            TransactionFieldValueTypeEnum.DateTime => !DateTime.TryParse(valueStr, out _),
            TransactionFieldValueTypeEnum.Int => !int.TryParse(valueStr, out _),
            TransactionFieldValueTypeEnum.Decimal => !decimal.TryParse(valueStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _),
            _ => false
        };
    }

    private static async Task<int> ApplyFieldRulesAsync(
        Order order,
        IEnumerable<OrderTransactionFieldRule> fieldRules,
        IReadOnlyDictionary<Guid, string> fieldRuleValues,
        CancellationToken ct)
    {
        var count = 0;
        foreach (var rule in fieldRules)
        {
            string? valueStr;
            if (rule.ValueSource == TransactionFieldValueSourceEnum.Fixed)
            {
                valueStr = rule.FixedValue;
            }
            else
            {
                if (!fieldRuleValues.TryGetValue(rule.Id, out valueStr))
                    continue;
            }

            var descriptor = OrderTransactionFieldRegistry.Get(rule.EntityPath);
            if (descriptor == null) continue;

            if (ApplyFieldValue(order, rule.EntityPath, valueStr, descriptor.ValueType))
                count++;
        }
        return await Task.FromResult(count);
    }

    private static bool ApplyFieldValue(Order order, string entityPath, string? valueStr, TransactionFieldValueTypeEnum valueType)
    {
        try
        {
            switch (entityPath)
            {
                case "Order.Quantity":
                    var qty = ParseInt(valueStr);
                    if (!qty.HasValue || order.Quantity == qty.Value) return false;
                    order.Quantity = qty.Value;
                    return true;
                case "Order.StatusId":
                    var statusId = ParseGuid(valueStr);
                    if (order.StatusId == statusId) return false;
                    order.StatusId = statusId;
                    return true;
                case "Order.ProductInfoId":
                    var productInfoId = ParseGuid(valueStr);
                    if (order.ProductInfoId == productInfoId) return false;
                    order.ProductInfoId = productInfoId;
                    return true;
                case "Order.RecipientId":
                    var recipientId = ParseGuid(valueStr);
                    if (order.RecipientId == recipientId) return false;
                    order.RecipientId = recipientId;
                    return true;
                case "Order.WarehouseInfoId":
                    var warehouseInfoId = ParseGuid(valueStr);
                    if (order.WarehouseInfoId == warehouseInfoId) return false;
                    order.WarehouseInfoId = warehouseInfoId;
                    return true;

                case "Shipment.PostingNumber" when order.Shipment != null:
                    if (order.Shipment.PostingNumber == (valueStr ?? "")) return false;
                    order.Shipment.PostingNumber = valueStr ?? "";
                    return true;
                case "Shipment.OrderNumber" when order.Shipment != null:
                    if (order.Shipment.OrderNumber == valueStr) return false;
                    order.Shipment.OrderNumber = valueStr;
                    return true;
                case "Shipment.StatusId" when order.Shipment != null:
                    var shipStatusId = ParseGuid(valueStr);
                    if (order.Shipment.StatusId == shipStatusId) return false;
                    order.Shipment.StatusId = shipStatusId;
                    return true;
                case "Shipment.DeliveryMethodId" when order.Shipment != null:
                    var dmId = ParseGuid(valueStr);
                    if (order.Shipment.DeliveryMethodId == dmId) return false;
                    order.Shipment.DeliveryMethodId = dmId;
                    return true;
                case "Shipment.TrackingNumber" when order.Shipment != null:
                    if (order.Shipment.TrackingNumber == valueStr) return false;
                    order.Shipment.TrackingNumber = valueStr;
                    return true;
                case "Shipment.ShipmentDate" when order.Shipment != null:
                    var shipDate = ParseDateTime(valueStr);
                    if (order.Shipment.ShipmentDate == shipDate) return false;
                    order.Shipment.ShipmentDate = shipDate;
                    return true;
                case "Shipment.InProcessAt" when order.Shipment != null:
                    var inProcessAt = ParseDateTime(valueStr);
                    if (order.Shipment.InProcessAt == inProcessAt) return false;
                    order.Shipment.InProcessAt = inProcessAt;
                    return true;

                case "OrderProductInfo.ProductId" when order.ProductInfo != null:
                    var productId = ParseGuid(valueStr);
                    if (order.ProductInfo.ProductId == productId) return false;
                    order.ProductInfo.ProductId = productId;
                    return true;
                case "OrderProductInfo.SupplierId" when order.ProductInfo != null:
                    var supplierId = ParseGuid(valueStr);
                    if (order.ProductInfo.SupplierId == supplierId) return false;
                    order.ProductInfo.SupplierId = supplierId;
                    return true;

                case "Recipient.Name" when order.Recipient != null:
                    if (order.Recipient.Name == valueStr) return false;
                    order.Recipient.Name = valueStr;
                    return true;
                case "Recipient.Phone" when order.Recipient != null:
                    if (order.Recipient.Phone == valueStr) return false;
                    order.Recipient.Phone = valueStr;
                    return true;
                case "Recipient.Email" when order.Recipient != null:
                    if (order.Recipient.Email == valueStr) return false;
                    order.Recipient.Email = valueStr;
                    return true;
                case "Recipient.AddressId" when order.Recipient != null:
                    var addressId = ParseGuid(valueStr);
                    if (order.Recipient.AddressId == addressId) return false;
                    order.Recipient.AddressId = addressId;
                    return true;

                case "WarehouseInfo.RecipientWarehouseId" when order.WarehouseInfo != null:
                    var rwId = ParseGuid(valueStr);
                    if (order.WarehouseInfo.RecipientWarehouseId == rwId) return false;
                    order.WarehouseInfo.RecipientWarehouseId = rwId;
                    return true;
                case "WarehouseInfo.SenderWarehouseId" when order.WarehouseInfo != null:
                    var swId = ParseGuid(valueStr);
                    if (order.WarehouseInfo.SenderWarehouseId == swId) return false;
                    order.WarehouseInfo.SenderWarehouseId = swId;
                    return true;

                default:
                    return false;
            }
        }
        catch
        {
            return false;
        }
    }

    private static int? ParseInt(string? s) => int.TryParse(s, out var v) ? v : null;
    private static Guid? ParseGuid(string? s) => string.IsNullOrWhiteSpace(s) ? null : (Guid.TryParse(s, out var g) ? g : null);
    private static DateTime? ParseDateTime(string? s) => string.IsNullOrWhiteSpace(s) ? null : (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null);

    private async Task<Dictionary<string, decimal>> BuildContextAsync(Order order, CancellationToken ct)
    {
        var context = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["Quantity"] = order.Quantity
        };

        foreach (var price in order.Prices)
        {
            if (price.PriceType == null) continue;
            var key = CalculationEngine.SanitizeKey(price.PriceType.Name ?? string.Empty);
            if (!string.IsNullOrEmpty(key))
                context[key] = price.Value;
        }

        var calcRules = await _db.CalculationRules
            .Where(r => !r.IsDeleted && r.IsActive)
            .OrderBy(r => r.SortOrder)
            .AsNoTracking()
            .ToListAsync(ct);

        if (calcRules.Count > 0)
        {
            var computed = _calcEngine.Calculate(order, calcRules);
            foreach (var kv in computed.Where(k => k.Value != decimal.MinValue))
            {
                var key = CalculationEngine.SanitizeKey(kv.Key);
                if (!string.IsNullOrEmpty(key))
                    context[key] = kv.Value;
            }
        }

        return context;
    }

    private async Task<bool> UpsertOrderPriceAsync(
        Guid orderId,
        Guid priceTypeId,
        Guid? currencyId,
        decimal value,
        CancellationToken ct)
    {
        var existing = await _db.OrderPrices
            .FirstOrDefaultAsync(p => p.OrderId == orderId && p.PriceTypeId == priceTypeId, ct);

        if (existing != null)
        {
            if (existing.Value == value && existing.CurrencyId == currencyId)
                return false;
            existing.Value = value;
            existing.CurrencyId = currencyId;
        }
        else
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

        return true;
    }

    private static string BuildFormulaBreakdown(string formula, IReadOnlyDictionary<string, decimal> context, decimal result) =>
        CalculationEngine.BuildFormulaBreakdown(formula, context, result);
}
