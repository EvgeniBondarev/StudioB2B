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
            ["/orders-sync"]         = ("Загрузка",       "cloud_sync"),
            ["/order-statuses"]      = ("Статусы",        "label"),
            ["/calculation-rules"]   = ("Расчёты",        "calculate"),
            ["/roles"]               = ("Роли",           "security"),
        };

    private readonly List<TabEntry> _tabs = [];

    public IReadOnlyList<TabEntry> Tabs => _tabs;

    public event Action? TabsChanged;

    public void EnsureOpen(string path)
    {
        if (!KnownPages.TryGetValue(path, out var info))
            return;

        if (_tabs.Any(t => t.Path == path))
        {
            TabsChanged?.Invoke();
            return;
        }

        _tabs.Add(new TabEntry(path, info.Title, info.Icon));
        TabsChanged?.Invoke();
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
