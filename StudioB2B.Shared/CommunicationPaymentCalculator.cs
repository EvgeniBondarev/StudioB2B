using StudioB2B.Domain.Constants;

namespace StudioB2B.Shared;

/// <summary>
/// Единая логика начисления по тарифам для доски и сервера.
/// Почасовка без порогов по времени — доля от часа. Если у правила задан Min/Max длительности — при попадании в диапазон
/// выплачивается полная сумма ставки (не доля часа), плюс суммируются другие подошедшие правила.
/// </summary>
public static class CommunicationPaymentCalculator
{
    public static decimal ComputeRateContribution(
        decimal totalMinutes,
        PaymentMode mode,
        decimal rate,
        bool hasDurationBounds)
    {
        return mode switch
        {
            PaymentMode.PerTask => rate,
            PaymentMode.Hourly when hasDurationBounds => rate,
            PaymentMode.Hourly => rate * (totalMinutes / 60m),
            _ => 0m
        };
    }

    /// <summary>
    /// Строки расшифровки по активным тарифам (те же фильтры, что и при начислении).
    /// </summary>
    public static List<PaymentBreakdownLineDto> ComputeBreakdownLines(
        decimal totalMinutes,
        CommunicationTaskType taskType,
        Guid? assignedUserId,
        IReadOnlyList<CommunicationPaymentRateDto> rates)
    {
        // Step 1: collect all eligible rates (type, user, duration filters)
        var matched = new List<CommunicationPaymentRateDto>();
        foreach (var rate in rates)
        {
            if (!rate.IsActive) continue;
            if (rate.TaskType.HasValue && rate.TaskType != taskType) continue;
            if (rate.UserId.HasValue && rate.UserId != assignedUserId) continue;
            if (rate.MinDurationMinutes.HasValue && totalMinutes < rate.MinDurationMinutes.Value) continue;
            if (rate.MaxDurationMinutes.HasValue && totalMinutes > rate.MaxDurationMinutes.Value) continue;
            matched.Add(rate);
        }

        // Step 2: priority — if any specific (TaskType != null) rates matched, general (TaskType == null) rates are excluded
        if (matched.Any(r => r.TaskType != null))
            matched = matched.Where(r => r.TaskType != null).ToList();

        // Step 3: build breakdown lines
        var lines = new List<PaymentBreakdownLineDto>();
        foreach (var rate in matched)
        {
            var hasBounds = rate.MinDurationMinutes.HasValue || rate.MaxDurationMinutes.HasValue;
            var amount = ComputeRateContribution(totalMinutes, rate.PaymentMode, rate.Rate, hasBounds);
            if (amount == 0m) continue;

            lines.Add(new PaymentBreakdownLineDto
            {
                Caption = BuildBreakdownCaption(rate, totalMinutes, hasBounds, amount),
                Amount = amount
            });
        }

        return lines;
    }

    private static string BuildBreakdownCaption(
        CommunicationPaymentRateDto r,
        decimal totalMinutes,
        bool hasBounds,
        decimal amount)
    {
        var typeScope = r.TaskType switch
        {
            null => "все типы",
            CommunicationTaskType.Chat => "чат",
            CommunicationTaskType.Question => "вопрос",
            CommunicationTaskType.Review => "отзыв",
            _ => "—"
        };

        var who = r.UserId.HasValue ? " · персонально" : "";
        var tier = "";
        if (r.MinDurationMinutes.HasValue || r.MaxDurationMinutes.HasValue)
        {
            var p = new List<string>();
            if (r.MinDurationMinutes.HasValue) p.Add($"от {r.MinDurationMinutes} мин");
            if (r.MaxDurationMinutes.HasValue) p.Add($"до {r.MaxDurationMinutes} мин");
            tier = " · " + string.Join(", ", p);
        }

        var note = string.IsNullOrWhiteSpace(r.Description) ? "" : $" · {r.Description}";

        if (r.PaymentMode == PaymentMode.PerTask)
            return $"За задачу · {typeScope}{who}{tier}{note} · {r.Rate:F0} ₽";

        if (hasBounds)
            return $"Почасовая (полная ставка за интервал) · {typeScope}{who}{tier}{note} · {r.Rate:F0} ₽ → {amount:F2} ₽";

        var hh = (int)totalMinutes / 60;
        var mm = (int)Math.Round(totalMinutes % 60, MidpointRounding.AwayFromZero);
        var timeStr = hh > 0 ? $"{hh} ч {mm} мин" : $"{mm} мин";
        return $"Почасовая · {typeScope}{who}{note} · {r.Rate:F0} ₽/ч × {timeStr} = {amount:F2} ₽";
    }
}
