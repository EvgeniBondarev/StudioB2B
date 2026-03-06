using Hangfire;
using StudioB2B.Domain.Entities.Orders;

namespace StudioB2B.Infrastructure.Features.Orders;

/// <summary>
/// Строит cron-выражение для Hangfire по параметрам <see cref="SyncJobSchedule"/>.
/// </summary>
public static class ScheduleCronBuilder
{
    public static string Build(SyncJobSchedule schedule) => schedule.ScheduleType switch
    {
        ScheduleType.EveryNMinutes => Cron.MinuteInterval(
            schedule.IntervalMinutes ?? throw new InvalidOperationException("IntervalMinutes required.")),

        ScheduleType.EveryNHours => Cron.HourInterval(
            schedule.IntervalHours ?? throw new InvalidOperationException("IntervalHours required.")),

        ScheduleType.EveryNDays => BuildEveryNDays(
            schedule.IntervalDays ?? throw new InvalidOperationException("IntervalDays required."),
            schedule.TimeOfDay    ?? TimeSpan.Zero),

        ScheduleType.DailyAt => Cron.Daily(
            (schedule.TimeOfDay ?? TimeSpan.Zero).Hours,
            (schedule.TimeOfDay ?? TimeSpan.Zero).Minutes),

        ScheduleType.WeeklyAt => BuildWeeklyAt(
            schedule.DaysOfWeek ?? throw new InvalidOperationException("DaysOfWeek required."),
            schedule.TimeOfDay  ?? TimeSpan.Zero),

        ScheduleType.MonthlyAt => BuildMonthlyAt(
            schedule.DayOfMonth ?? throw new InvalidOperationException("DayOfMonth required."),
            schedule.TimeOfDay  ?? TimeSpan.Zero),

        ScheduleType.CustomCron => !string.IsNullOrWhiteSpace(schedule.CronExpression)
            ? schedule.CronExpression
            : throw new InvalidOperationException("CronExpression required."),

        _ => throw new InvalidOperationException($"Unknown ScheduleType: {schedule.ScheduleType}")
    };

    private static string BuildEveryNDays(int days, TimeSpan time)
    {
        if (days < 1) throw new ArgumentOutOfRangeException(nameof(days));
        // cron: мин час */дней * *
        return $"{time.Minutes} {time.Hours} */{days} * *";
    }

    private static string BuildWeeklyAt(string daysOfWeek, TimeSpan time)
    {
        // daysOfWeek = "1,3,5"  (пн,ср,пт)
        var parts = daysOfWeek.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) throw new InvalidOperationException("At least one day of week required.");
        var dow = string.Join(",", parts);
        return $"{time.Minutes} {time.Hours} * * {dow}";
    }

    private static string BuildMonthlyAt(int day, TimeSpan time)
    {
        if (day is < 1 or > 28)
            throw new ArgumentOutOfRangeException(nameof(day), "Day of month must be between 1 and 28.");
        return $"{time.Minutes} {time.Hours} {day} * *";
    }

    /// <summary>Возвращает человекочитаемое описание расписания на русском.</summary>
    public static string Describe(SyncJobSchedule s) => s.ScheduleType switch
    {
        ScheduleType.EveryNMinutes => $"Каждые {s.IntervalMinutes} мин.",
        ScheduleType.EveryNHours   => $"Каждые {s.IntervalHours} ч.",
        ScheduleType.EveryNDays    => $"Каждые {s.IntervalDays} дн. в {s.TimeOfDay:hh\\:mm}",
        ScheduleType.DailyAt       => $"Ежедневно в {s.TimeOfDay:hh\\:mm}",
        ScheduleType.WeeklyAt      => $"По {DescribeDaysOfWeek(s.DaysOfWeek)} в {s.TimeOfDay:hh\\:mm}",
        ScheduleType.MonthlyAt     => $"{s.DayOfMonth}-го числа в {s.TimeOfDay:hh\\:mm}",
        ScheduleType.CustomCron    => $"Cron: {s.CronExpression}",
        _                          => "—"
    };

    private static string DescribeDaysOfWeek(string? dow)
    {
        if (string.IsNullOrEmpty(dow)) return "?";
        var names = new[] { "вс", "пн", "вт", "ср", "чт", "пт", "сб" };
        var parts = dow.Split(',')
            .Select(p => int.TryParse(p.Trim(), out var d) && d < names.Length ? names[d] : p);
        return string.Join(", ", parts);
    }
}
