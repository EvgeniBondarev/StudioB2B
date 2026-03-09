using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioB2B.Application.Common;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Domain.Entities.Orders;
using StudioB2B.Infrastructure.Features.Orders;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.Services;

public class OrderTransactionService : IOrderTransactionService
{
    private readonly TenantDbContext _db;
    private readonly CalculationEngine _calcEngine;
    private readonly ILogger<OrderTransactionService> _logger;

    public OrderTransactionService(
        TenantDbContext db,
        CalculationEngine calcEngine,
        ILogger<OrderTransactionService> logger)
    {
        _db = db;
        _calcEngine = calcEngine;
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
            if (rule.ValueSource == TransactionValueSource.Formula && !string.IsNullOrWhiteSpace(rule.Formula))
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
                FormulaBreakdown = breakdown
            });
        }

        return new TransactionApplyPreview
        {
            TransactionName = transaction.Name,
            ToStatusName = transaction.ToSystemStatus?.Name ?? "",
            Rules = rules
        };
    }

    public async Task<TransactionApplyPreview?> GetApplyPreviewWithUserValuesAsync(Guid orderId, Guid transactionId, IReadOnlyDictionary<Guid, decimal> userValues, CancellationToken ct = default)
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

            var priceKey = CalculationEngine.SanitizeKey(rule.PriceType.Name ?? string.Empty);

            if (rule.ValueSource == TransactionValueSource.UserInput)
            {
                if (userValues.TryGetValue(rule.Id, out var uv) && !string.IsNullOrEmpty(priceKey))
                    context[priceKey] = uv;
                rules.Add(new TransactionApplyRulePreview
                {
                    RuleId = rule.Id,
                    PriceTypeId = rule.PriceTypeId,
                    PriceTypeName = rule.PriceType.Name ?? "",
                    ProductId = rule.ProductId,
                    ProductName = rule.Product?.Name,
                    ValueSource = rule.ValueSource
                });
            }
            else if (rule.ValueSource == TransactionValueSource.Formula && !string.IsNullOrWhiteSpace(rule.Formula))
            {
                decimal? computed = null;
                string? breakdown = null;
                try
                {
                    computed = _calcEngine.EvaluateFormula(rule.Formula, context);
                    if (computed.HasValue)
                    {
                        breakdown = BuildFormulaBreakdown(rule.Formula, context, computed.Value);
                        if (!string.IsNullOrEmpty(priceKey))
                            context[priceKey] = computed.Value;
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
                    FormulaBreakdown = breakdown
                });
            }
        }

        return new TransactionApplyPreview
        {
            TransactionName = transaction.Name,
            ToStatusName = transaction.ToSystemStatus?.Name ?? "",
            Rules = rules
        };
    }

    public async Task<TransactionApplyResult> ApplyAsync(Guid orderId, Guid transactionId, IReadOnlyDictionary<Guid, decimal>? ruleValues = null, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .IncludeForGrid()
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
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == transactionId && !t.IsDeleted, ct);

        if (transaction == null)
            return new TransactionApplyResult { Success = false, ErrorMessage = "Транзакция не найдена" };

        if (!transaction.IsEnabled)
            return new TransactionApplyResult { Success = false, ErrorMessage = "Транзакция отключена" };

        if (order.SystemStatusId != transaction.FromSystemStatusId)
            return new TransactionApplyResult
            {
                Success = false,
                ErrorMessage = $"Текущий статус заказа ({order.SystemStatus?.Name ?? "—"}) не совпадает с исходным статусом транзакции ({transaction.FromSystemStatus?.Name ?? "—"})"
            };

        var context = await BuildContextAsync(order, ct);
        var pricesUpdated = 0;

        foreach (var rule in transaction.Rules.OrderBy(r => r.SortOrder))
        {
            if (rule.PriceType == null)
                continue;

            if (rule.ProductId.HasValue && order.ProductInfo?.ProductId != rule.ProductId.Value)
                continue;

            decimal value;
            if (rule.ValueSource == TransactionValueSource.UserInput)
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

        order.SystemStatusId = transaction.ToSystemStatusId;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Transaction {TransactionName} applied to order {OrderId}: {PricesUpdated} prices updated, status -> {ToStatus}",
            transaction.Name, orderId, pricesUpdated, transaction.ToSystemStatus?.Name);

        return new TransactionApplyResult
        {
            Success = true,
            PricesUpdated = pricesUpdated
        };
    }

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

    private static string BuildFormulaBreakdown(string formula, IReadOnlyDictionary<string, decimal> context, decimal result)
    {
        var expanded = formula;
        foreach (var kv in context.OrderByDescending(x => x.Key.Length))
        {
            expanded = System.Text.RegularExpressions.Regex.Replace(
                expanded,
                $@"\b{System.Text.RegularExpressions.Regex.Escape(kv.Key)}\b",
                kv.Value.ToString("G29", CultureInfo.InvariantCulture),
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        return $"{expanded} = {result.ToString("G29", CultureInfo.InvariantCulture)}";
    }
}
