using StudioB2B.Domain.Constants;

namespace StudioB2B.Shared;

/// <summary>
/// Единая логика начисления по тарифам для доски и сервера.
///
/// PaymentMode.PerTask — фиксированная ставка за задачу, не зависит от времени.
///
/// PaymentMode.Hourly:
///   · MinDurationMinutes — пол биллинга: если отработано меньше минимума,
///     effectiveMinutes = Min (billing floor).
///   · MaxDurationMinutes — потолок биллинга: если отработано больше максимума,
///     effectiveMinutes = Max (выплачивается полная ставка).
///     amount = Rate × (effectiveMinutes / Max).
///   · Без MaxDurationMinutes — стандартная почасовка: Rate × (effectiveMinutes / 60).
/// </summary>
public static class CommunicationPaymentCalculator
{
    public static decimal ComputeRateContribution(
        decimal effectiveMinutes,
        PaymentMode mode,
        decimal rate,
        int? maxDurationMinutes)
    {
        return mode switch
        {
            PaymentMode.PerTask => rate,
            PaymentMode.Hourly => rate * (effectiveMinutes / 60m),
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
        // Step 1: collect all eligible rates.
        // MinDurationMinutes — пол биллинга: если не достигнуто, effective = Min.
        // MaxDurationMinutes — потолок биллинга: если превышено, effective = Max (полная ставка).
        var matched = new List<(CommunicationPaymentRateDto Rate, decimal EffectiveMinutes)>();
        foreach (var rate in rates)
        {
            if (!rate.IsActive) continue;
            if (rate.TaskType.HasValue && rate.TaskType != taskType) continue;
            if (rate.UserId.HasValue && rate.UserId != assignedUserId) continue;

            var effective = totalMinutes;
            if (rate.MinDurationMinutes.HasValue && effective < rate.MinDurationMinutes.Value)
                effective = rate.MinDurationMinutes.Value;
            if (rate.MaxDurationMinutes.HasValue && effective > rate.MaxDurationMinutes.Value)
                effective = rate.MaxDurationMinutes.Value;
            matched.Add((rate, effective));
        }

        // Step 2: priority — если есть специфичные (TaskType != null) тарифы, общие (TaskType == null) исключаются
        if (matched.Any(x => x.Rate.TaskType != null))
            matched = matched.Where(x => x.Rate.TaskType != null).ToList();

        // Step 3: build breakdown lines
        var lines = new List<PaymentBreakdownLineDto>();
        foreach (var (rate, effectiveMinutes) in matched)
        {
            var amount = ComputeRateContribution(effectiveMinutes, rate.PaymentMode, rate.Rate, rate.MaxDurationMinutes);
            if (amount == 0m) continue;

            var atFloor = rate.MinDurationMinutes.HasValue && totalMinutes < rate.MinDurationMinutes.Value;
            var atCeiling = rate.MaxDurationMinutes.HasValue && totalMinutes > rate.MaxDurationMinutes.Value;
            lines.Add(new PaymentBreakdownLineDto
            {
                Caption = BuildBreakdownCaption(rate, effectiveMinutes, amount, atFloor, atCeiling),
                Details = BuildBreakdownDetails(rate, totalMinutes, effectiveMinutes, amount, atFloor, atCeiling),
                Amount = amount
            });
        }

        return lines;
    }

    private static string BuildBreakdownCaption(
        CommunicationPaymentRateDto r,
        decimal effectiveMinutes,
        decimal amount,
        bool atFloor,
        bool atCeiling)
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
        var floorNote = atFloor ? $" (мин. биллинг {r.MinDurationMinutes} мин)" : "";
        var ceilNote = atCeiling ? $" (макс. биллинг {r.MaxDurationMinutes} мин)" : "";
        var boundNote = floorNote + ceilNote;

        if (r.PaymentMode == PaymentMode.PerTask)
            return $"За задачу · {typeScope}{who}{tier}{note} · {r.Rate:F0} ₽";

        if (r.MaxDurationMinutes.HasValue)
        {
            var em = (int)Math.Round(effectiveMinutes, MidpointRounding.AwayFromZero);
            return $"Почасовая{boundNote} · {typeScope}{who}{tier}{note} · {r.Rate:F0} ₽/ч × {em} мин = {amount:F2} ₽";
        }

        var hh = (int)effectiveMinutes / 60;
        var mm = (int)Math.Round(effectiveMinutes % 60, MidpointRounding.AwayFromZero);
        var timeStr = hh > 0 ? $"{hh} ч {mm} мин" : $"{mm} мин";
        return $"Почасовая{boundNote} · {typeScope}{who}{note} · {r.Rate:F0} ₽/ч × {timeStr} = {amount:F2} ₽";
    }

    private static string BuildBreakdownDetails(
        CommunicationPaymentRateDto r,
        decimal totalMinutes,
        decimal effectiveMinutes,
        decimal amount,
        bool atFloor,
        bool atCeiling)
    {
        var factRounded = (int)Math.Round(totalMinutes, MidpointRounding.AwayFromZero);
        var effRounded = (int)Math.Round(effectiveMinutes, MidpointRounding.AwayFromZero);
        var minStr = r.MinDurationMinutes?.ToString() ?? "—";
        var maxStr = r.MaxDurationMinutes?.ToString() ?? "—";
        var clampMark = atFloor ? "min" : atCeiling ? "max" : "ok";

        if (r.PaymentMode == PaymentMode.PerTask)
        {
            return $"PerTask | факт {factRounded}м | диапазон {minStr}-{maxStr}м ({clampMark}) | сумма {r.Rate:F2} ₽";
        }

        var minuteRate = r.Rate / 60m;
        return $"Hourly | {r.Rate:F2} ₽/ч ({minuteRate:F4} ₽/м) | факт {factRounded}м -> оплачено {effRounded}м | диапазон {minStr}-{maxStr}м ({clampMark}) | {r.Rate:F2}/60×{effRounded}={amount:F2} ₽";
    }
}
