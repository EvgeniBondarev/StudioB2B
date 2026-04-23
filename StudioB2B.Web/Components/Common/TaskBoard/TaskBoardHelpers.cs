using StudioB2B.Domain.Constants;
using StudioB2B.Shared;

namespace StudioB2B.Web.Components.Common.TaskBoard;

public static class TaskBoardHelpers
{
    public static string GetTypeClass(CommunicationTaskType type) => type switch
    {
        CommunicationTaskType.Chat => "task-type-chat",
        CommunicationTaskType.Question => "task-type-question",
        CommunicationTaskType.Review => "task-type-review",
        _ => ""
    };

    public static string GetPreviewTypeClass(CommunicationTaskType type) => type switch
    {
        CommunicationTaskType.Chat => "preview-chat",
        CommunicationTaskType.Question => "preview-question",
        CommunicationTaskType.Review => "preview-review",
        _ => ""
    };

    public static string GetTypeLabel(CommunicationTaskType type) => type switch
    {
        CommunicationTaskType.Chat => "Чат",
        CommunicationTaskType.Question => "Вопрос",
        CommunicationTaskType.Review => "Отзыв",
        _ => "—"
    };

    public static string GetStatusLabel(CommunicationTaskStatus status) => status switch
    {
        CommunicationTaskStatus.New => "Новая",
        CommunicationTaskStatus.InProgress => "В работе",
        CommunicationTaskStatus.Done => "Выполнена",
        CommunicationTaskStatus.Cancelled => "Отменена",
        _ => "—"
    };

    public static string FormatTimeAgo(DateTime utcTime)
    {
        var diff = DateTime.UtcNow - utcTime;
        if (diff.TotalSeconds < 60) return "только что";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} мин";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} ч";
        if (diff.TotalDays < 2) return "вчера";
        return utcTime.ToLocalTime().ToString("dd.MM");
    }

    public static string FormatDuration(DateTime? startedAt, long accumulatedTicks = 0)
    {
        var ticks = accumulatedTicks;
        if (startedAt.HasValue)
            ticks += (DateTime.UtcNow - startedAt.Value).Ticks;
        if (ticks <= 0) return "0с";
        var ts = TimeSpan.FromTicks(ticks);
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}ч {ts.Minutes:D2}м {ts.Seconds:D2}с";
        if (ts.TotalMinutes >= 1) return $"{(int)ts.TotalMinutes}м {ts.Seconds:D2}с";
        return $"{(int)ts.TotalSeconds}с";
    }

    public static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}ч {ts.Minutes}м";
        return $"{(int)ts.TotalMinutes}м";
    }

    public static string FormatOverlayDuration(CommunicationTaskDto task)
    {
        var ticks = task.TotalTimeSpentTicks;
        if (task.HasActiveTimer && task.StartedAt.HasValue)
            ticks += (DateTime.UtcNow - task.StartedAt.Value).Ticks;
        var ts = TimeSpan.FromTicks(ticks);
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}ч {ts.Minutes:D2}м {ts.Seconds:D2}с";
        if (ts.TotalMinutes >= 1) return $"{(int)ts.TotalMinutes}м {ts.Seconds:D2}с";
        return $"{(int)ts.TotalSeconds}с";
    }

    public static string FormatRateDescription(CommunicationPaymentRateDto r)
    {
        var parts = new List<string> { r.PaymentMode == PaymentMode.PerTask ? "За задачу" : "Почасовая" };
        if (r.MinDurationMinutes.HasValue && r.MaxDurationMinutes.HasValue)
            parts.Add($"если {r.MinDurationMinutes}–{r.MaxDurationMinutes} мин");
        else if (r.MinDurationMinutes.HasValue)
            parts.Add($"от {r.MinDurationMinutes} мин");
        else if (r.MaxDurationMinutes.HasValue)
            parts.Add($"до {r.MaxDurationMinutes} мин");
        if (!string.IsNullOrWhiteSpace(r.Description))
            parts.Add(r.Description);
        return string.Join(" · ", parts);
    }

    public static string FormatRateAmount(CommunicationPaymentRateDto r) =>
        r.PaymentMode == PaymentMode.PerTask ? $"{r.Rate:F0} ₽" : $"{r.Rate:F0} ₽/ч";

    public static string Plural(int n, string one, string few, string many)
    {
        var mod10 = n % 10;
        var mod100 = n % 100;
        if (mod100 is >= 11 and <= 19) return many;
        return mod10 switch { 1 => one, >= 2 and <= 4 => few, _ => many };
    }
}

