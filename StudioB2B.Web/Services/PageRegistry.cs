namespace StudioB2B.Web.Services;

public class PageRegistry
{
    public static readonly IReadOnlyDictionary<string, (string Title, string Icon)> Pages =
        new Dictionary<string, (string Title, string Icon)>
        {
            ["/"] = ("Главная", "home"),
            ["/users"] = ("Пользователи", "people"),
            ["/marketplace-clients"] = ("Маркетплейс", "store"),
            ["/orders"] = ("Заказы", "receipt_long"),
            ["/returns"] = ("Возвраты", "assignment_return"),
            ["/orders/apply-transaction"] = ("Провести документ", "swap_horiz"),
            ["/orders-sync"] = ("Загрузка", "cloud_sync"),
            ["/order-statuses"] = ("Статусы", "label"),
            ["/price-types"] = ("Типы цен", "payments"),
            ["/transactions"] = ("Документы", "swap_horiz"),
            ["/transactions-history"] = ("История проведений", "schedule"),
            ["/calculation-rules"] = ("Расчёты", "calculate"),
            ["/audit"] = ("Журнал", "history"),
            ["/roles"] = ("Роли", "security"),
            ["/communication"] = ("Коммуникации", "communication"),
            ["/chats"] = ("Чаты", "chat"),
            ["/questions"] = ("Вопросы", "help"),
            ["/reviews"] = ("Отзывы", "star"),
            ["/task-board"] = ("Доска задач", "view_kanban"),
            ["/task-board-settings"] = ("Настройки оплаты", "tune"),
            ["/task-board-report"] = ("Отчёт по оплате", "summarize"),
            ["/modules"] = ("Модули", "extension"),
            ["/permissions"] = ("Права доступа", "shield"),
            ["/ozon-push"] = ("Push-уведомления Ozon", "notifications"),
            ["/my-tenants"] = ("Рабочие пространства", "domain"),
            ["/master-users"] = ("Пользователи", "manage_accounts"),
        };

    private static readonly (string Title, string Icon) Default = ("Страница", "article");

    public (string Title, string Icon) Get(string path)
    {
        var normalized = path.TrimEnd('/').ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized)) normalized = "/";

        // Strip query string for lookup
        var qIdx = normalized.IndexOf('?');
        if (qIdx >= 0) normalized = normalized[..qIdx];

        return Pages.TryGetValue(normalized, out var info) ? info : Default;
    }
}
