using System.Text;
using DynamicExpresso;
using StudioB2B.Domain.Entities.Orders;

namespace StudioB2B.Infrastructure.Services;

/// <summary>
/// Движок вычисления пользовательских формул на базе DynamicExpresso.
/// Переменные в контексте берутся из <see cref="Order.Prices"/> по ключу PriceType.Name
/// (пробелы убираются → CamelCase), а также базовое поле Quantity.
/// </summary>
public class CalculationEngine
{
    private readonly Interpreter _interpreter = new(InterpreterOptions.Default);

    private readonly Dictionary<string, string> _errors = new();

    /// <summary>
    /// Ошибки вычисления последнего вызова <see cref="Calculate"/>:
    /// ResultKey → сообщение об ошибке.
    /// </summary>
    public IReadOnlyDictionary<string, string> LastErrors => _errors;

    /// <summary>
    /// Вычислить все активные правила для одного заказа.
    /// При ошибке вычисления значение равно <see cref="decimal.MinValue"/>;
    /// подробности доступны через <see cref="LastErrors"/>.
    /// </summary>
    /// <param name="order">Заказ с загруженными Prices → PriceType.</param>
    /// <param name="rules">Список активных правил, отсортированный по SortOrder.</param>
    /// <returns>Словарь ResultKey → результат вычисления.</returns>
    public Dictionary<string, decimal> Calculate(Order order, IEnumerable<CalculationRule> rules)
    {
        _errors.Clear();

        var context = BuildContext(order);
        var results = new Dictionary<string, decimal>();

        foreach (var rule in rules.OrderBy(r => r.SortOrder))
        {
            if (string.IsNullOrWhiteSpace(rule.Formula))
                continue;

            try
            {
                var parameters = context
                    .Select(kv => new Parameter(kv.Key, typeof(decimal), kv.Value))
                    .ToArray();

                var result = _interpreter.Eval(rule.Formula, parameters);
                var decimalResult = Convert.ToDecimal(result);

                results[rule.ResultKey] = decimalResult;

                // Добавляем результат в контекст — последующие формулы могут его использовать.
                var sanitizedKey = SanitizeKey(rule.ResultKey);
                if (!string.IsNullOrEmpty(sanitizedKey))
                    context[sanitizedKey] = decimalResult;
            }
            catch (Exception ex)
            {
                results[rule.ResultKey] = decimal.MinValue;
                _errors[rule.ResultKey] = ex.Message;
            }
        }

        return results;
    }

    /// <summary>
    /// Возвращает список доступных имён переменных для данного заказа (для отображения в UI).
    /// </summary>
    public static IReadOnlyList<string> GetAvailableVariables(Order order) =>
        BuildContext(order).Keys.ToList();

    /// <summary>
    /// Возвращает снимок контекста переменных для заказа:
    /// имена переменных → их числовые значения.
    /// Используется для отладки и визуализации в UI.
    /// </summary>
    public static IReadOnlyDictionary<string, decimal> GetContextSnapshot(Order order) =>
        BuildContext(order);

    /// <summary>
    /// Возвращает предустановленные имена переменных (без конкретного заказа) —
    /// используется при отображении подсказок в конструкторе правил.
    /// </summary>
    public static IReadOnlyList<string> GetBaseVariableNames(IEnumerable<string> priceTypeNames)
    {
        var names = new List<string> { "Quantity" };
        names.AddRange(priceTypeNames.Select(SanitizeKey));
        return names.Distinct().ToList();
    }

    /// <summary>
    /// Вычислить формулу с заданным контекстом переменных.
    /// Используется для правил транзакций.
    /// </summary>
    public decimal EvaluateFormula(string formula, IReadOnlyDictionary<string, decimal> context)
    {
        var parameters = context
            .Select(kv => new Parameter(kv.Key, typeof(decimal), kv.Value))
            .ToArray();
        var result = _interpreter.Eval(formula, parameters);
        return Convert.ToDecimal(result);
    }

    /// <summary>
    /// Попытка вычислить формулу на тестовых нулевых значениях переменных.
    /// Возвращает null при успехе, сообщение об ошибке при неудаче.
    /// </summary>
    public string? ValidateFormula(string formula, IEnumerable<string> variableNames)
    {
        try
        {
            var paramDefs = variableNames
                .Select(k => new Parameter(k, typeof(decimal), 0m))
                .ToArray();
            _interpreter.Eval(formula, paramDefs);
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private static Dictionary<string, decimal> BuildContext(Order order)
    {
        var context = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["Quantity"] = order.Quantity
        };

        foreach (var price in order.Prices)
        {
            if (price.PriceType == null)
                continue;

            var key = SanitizeKey(price.PriceType.Name ?? string.Empty);
            if (!string.IsNullOrEmpty(key))
                context[key] = price.Value;
        }

        return context;
    }

    /// <summary>
    /// Преобразует строку в валидный идентификатор: убирает пробелы, делает CamelCase,
    /// оставляет только буквы, цифры и нижнее подчёркивание.
    /// Например: "Цена до скидки" → "ЦенаДоСкидки"
    /// </summary>
    public static string SanitizeKey(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();

        foreach (var (part, index) in parts.Select((p, i) => (p, i)))
        {
            if (part.Length == 0) continue;

            var filtered = new string(part.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
            if (filtered.Length == 0) continue;

            if (index == 0)
                sb.Append(filtered);
            else
                sb.Append(char.ToUpper(filtered[0]) + filtered[1..]);
        }

        return sb.ToString();
    }
}
