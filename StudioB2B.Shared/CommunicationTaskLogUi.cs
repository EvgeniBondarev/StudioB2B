using System.Globalization;
using System.Text.RegularExpressions;

namespace StudioB2B.Shared;

/// <summary>Подписи журнала задач и форматирование для интерфейса (ru-RU).</summary>
public static class CommunicationTaskLogUi
{
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

    /// <summary>Человекочитаемое название действия на русском.</summary>
    public static string GetActionRussian(string? action) =>
        action switch
        {
            "Assigned" => "Назначена исполнителю",
            "Started" => "Работа начата",
            "Paused" => "Пауза",
            "Resumed" => "Работа продолжена",
            "Completed" => "Завершена",
            "Reopened" => "Возвращена в работу",
            "AutoReopened" => "Автоматически возвращена в очередь",
            "Created" => "Создана",
            null or "" => "—",
            _ => action
        };

    /// <summary>Форматирует длительность сессии для ru-RU (десятичная запятая).</summary>
    public static string FormatSessionMinutesRu(double minutes) =>
        minutes.ToString("F1", Russian);

    /// <summary>
    /// Локализует текст деталей записи журнала (в т.ч. старый формат TotalTime/Payment на английском).
    /// </summary>
    public static string FormatDetailsRussian(string? details)
    {
        if (string.IsNullOrWhiteSpace(details))
            return "";

        if (details.Contains("начислено:", StringComparison.OrdinalIgnoreCase))
            return details.Trim();

        var m = Regex.Match(
            details.Trim(),
            @"^TotalTime:\s*(?<time>[^,]+),\s*Payment:\s*(?<pay>.+)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success)
            return details.Trim();

        var time = m.Groups["time"].Value.Trim();
        var payRaw = m.Groups["pay"].Value.Trim();
        if (TryParseMoney(payRaw, out var pay))
            return $"Время в работе: {time}, начислено: {pay.ToString("N2", Russian)} ₽";

        return $"Время в работе: {time}, начислено: {payRaw} ₽";
    }

    private static bool TryParseMoney(string raw, out decimal value)
    {
        raw = raw.Trim();
        if (decimal.TryParse(raw, NumberStyles.Number, Russian, out value))
            return true;
        if (decimal.TryParse(raw.Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture, out value))
            return true;
        return false;
    }
}
