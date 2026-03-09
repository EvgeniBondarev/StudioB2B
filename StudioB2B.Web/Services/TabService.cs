namespace StudioB2B.Web.Services;

public record TabEntry(string Path, string Title, string Icon);

public class TabService
{
    public static readonly IReadOnlyDictionary<string, (string Title, string Icon)> KnownPages =
        new Dictionary<string, (string Title, string Icon)>
        {
            ["/"]                    = ("Главная",       "home"),
            ["/users"]               = ("Пользователи",  "people"),
            ["/marketplace-clients"] = ("Маркетплейс",   "store"),
            ["/orders"]              = ("Заказы",         "receipt_long"),
            ["/orders/apply-transaction"] = ("Транзакция заказов", "swap_horiz"),
            ["/orders-sync"]         = ("Загрузка",       "cloud_sync"),
            ["/order-statuses"]      = ("Статусы",        "label"),
            ["/price-types"]         = ("Типы цен",       "payments"),
            ["/transactions"]        = ("Транзакции",     "swap_horiz"),
            ["/transactions-history"] = ("История транзакций", "schedule"),
            ["/calculation-rules"]   = ("Расчёты",        "calculate"),
            ["/audit"]               = ("Журнал",         "history"),
            ["/roles"]               = ("Роли",           "security"),
            ["/chats"]               = ("Чаты",           "chat"),
        };

    private readonly List<TabEntry> _tabs = [];

    public IReadOnlyList<TabEntry> Tabs => _tabs;

    public event Action? TabsChanged;

    public void EnsureOpen(string path)
    {
        if (!KnownPages.TryGetValue(path, out var info))
            return;

        if (_tabs.Any(t => GetBasePath(t.Path) == GetBasePath(path)))
        {
            TabsChanged?.Invoke();
            return;
        }

        _tabs.Add(new TabEntry(path, info.Title, info.Icon));
        TabsChanged?.Invoke();
    }

    /// <summary>Opens a tab with custom path and title (e.g. for pages with query params).</summary>
    public void OpenTab(string path, string title, string icon = "swap_horiz")
    {
        if (_tabs.Any(t => GetBasePath(t.Path) == GetBasePath(path)))
        {
            TabsChanged?.Invoke();
            return;
        }

        _tabs.Add(new TabEntry(path, title, icon));
        TabsChanged?.Invoke();
    }

    private static string GetBasePath(string path)
    {
        var idx = path.IndexOf('?');
        return idx >= 0 ? path[..idx] : path;
    }

    public string? Close(string path)
    {
        var idx = _tabs.FindIndex(t => t.Path == path);
        if (idx < 0) return null;

        _tabs.RemoveAt(idx);
        TabsChanged?.Invoke();

        if (_tabs.Count == 0) return "/";

        // Navigate to the next tab, or the one before if this was the last
        var newIdx = Math.Min(idx, _tabs.Count - 1);
        return _tabs[newIdx].Path;
    }

    /// <summary>Replaces the current tab list (used to restore from localStorage).</summary>
    public void Restore(IEnumerable<TabEntry> tabs)
    {
        _tabs.Clear();
        _tabs.AddRange(tabs);
        TabsChanged?.Invoke();
    }
}
