namespace StudioB2B.Domain.Entities.Orders;

public enum ScheduleType
{
    /// <summary>Каждые N минут.</summary>
    EveryNMinutes = 1,

    /// <summary>Каждые N часов.</summary>
    EveryNHours = 2,

    /// <summary>Каждые N дней в заданное время.</summary>
    EveryNDays = 3,

    /// <summary>Каждый день в заданное время.</summary>
    DailyAt = 4,

    /// <summary>По дням недели в заданное время.</summary>
    WeeklyAt = 5,

    /// <summary>Каждый месяц в заданный день и время.</summary>
    MonthlyAt = 6,

    /// <summary>Произвольное cron-выражение.</summary>
    CustomCron = 7,
}
