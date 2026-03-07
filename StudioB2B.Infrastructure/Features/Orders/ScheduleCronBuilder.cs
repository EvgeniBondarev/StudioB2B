using StudioB2B.Domain.Entities.Orders;

namespace StudioB2B.Infrastructure.Features.Orders;

/// <summary>
/// Вспомогательный класс для работы с cron-расписаниями.
/// </summary>
public static class ScheduleCronBuilder
{
    /// <summary>
    /// Возвращает человекочитаемое описание cron-выражения на русском языке.
    /// </summary>
    public static string Describe(SyncJobSchedule s)
        => DescribeCron(s.CronExpression);

    /// <summary>
    /// Парсит стандартное 5-польное cron-выражение и возвращает описание на русском.
    /// </summary>
    public static string DescribeCron(string? cron)
    {
        if (string.IsNullOrWhiteSpace(cron)) return "—";

        try
        {
            var parts = cron.Trim().Split(' ');
            if (parts.Length != 5) return $"Cron: {cron}";

            var (min, hour, dom, month, dow) = (parts[0], parts[1], parts[2], parts[3], parts[4]);

            // Каждые N минут: */N * * * *
            if (min.StartsWith("*/") && hour == "*" && dom == "*" && month == "*" && dow == "*")
                return $"Каждые {min[2..]} мин.";

            // Каждые N часов: 0 */N * * *
            if (hour.StartsWith("*/") && dom == "*" && month == "*" && dow == "*")
                return $"Каждые {hour[2..]} ч. в :{min.PadLeft(2, '0')}";

            // Каждые N дней: M H */N * *
            if (dom.StartsWith("*/") && month == "*" && dow == "*")
                return $"Каждые {dom[2..]} дн. в {hour.PadLeft(2, '0')}:{min.PadLeft(2, '0')}";

            // По дням недели: M H * * d[,d]
            if (dom == "*" && month == "*" && dow != "*")
            {
                var time = $"{hour.PadLeft(2, '0')}:{min.PadLeft(2, '0')}";
                if (dow == "1-5") return $"Пн–Пт в {time}";
                if (dow == "0,6" || dow == "6,0") return $"Сб, Вс в {time}";
                return $"По {DescribeDow(dow)} в {time}";
            }

            // Ежедневно: M H * * *
            if (dom == "*" && month == "*" && dow == "*")
                return $"Ежедневно в {hour.PadLeft(2, '0')}:{min.PadLeft(2, '0')}";

            // Раз в месяц: M H D * *
            if (month == "*" && dow == "*" && !dom.Contains('*') && !dom.Contains('/'))
                return $"{dom}-го числа в {hour.PadLeft(2, '0')}:{min.PadLeft(2, '0')}";

            return $"Cron: {cron}";
        }
        catch
        {
            return $"Cron: {cron}";
        }
    }

    private static string DescribeDow(string dow)
    {
        var names = new[] { "Вс", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб" };
        var parts = dow.Split(',');
        var labels = parts.Select(p =>
            int.TryParse(p.Trim(), out var d) && d < names.Length ? names[d] : p);
        return string.Join(", ", labels);
    }
}
