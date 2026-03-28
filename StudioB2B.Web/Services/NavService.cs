namespace StudioB2B.Web.Services;

public class NavItem
{
    public string Path { get; set; } = string.Empty;
    public string? Role { get; set; }
}

public class NavGroup
{
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public List<NavItem> Items { get; set; } = new();
}

public class NavService
{
    public List<NavGroup> Groups { get; } = new()
    {
        new NavGroup
        {
            Title = "Главная",
            Icon = "home",
            Items = new() { new NavItem { Path = "/" } }
        },
        new NavGroup
        {
            Title = "Заказы",
            Icon = "receipt_long",
            Items = new()
            {
                new NavItem { Path = "/orders", Role = "OrdersView" },
                new NavItem { Path = "/returns", Role = "ReturnsView" },
                new NavItem { Path = "/transactions", Role = "TransactionsView" },
                new NavItem { Path = "/orders/apply-transaction", Role = "TransactionsView" },
                new NavItem { Path = "/transactions-history", Role = "TransactionsView" }
            }
        },
        new NavGroup
        {
            Title = "Маркетплейс",
            Icon = "storefront",
            Items = new()
            {
                new NavItem { Path = "/marketplace-clients", Role = "MarketplaceClientsView" },
                new NavItem { Path = "/price-types", Role = "PriceTypesView" },
                new NavItem { Path = "/calculation-rules", Role = "CalculationRulesView" },
                new NavItem { Path = "/order-statuses", Role = "OrderStatusesView" }
            }
        },
        new NavGroup
        {
            Title = "Коммуникации",
            Icon = "forum",
            Items = new()
            {
                new NavItem { Path = "/communication", Role = "ChatsView" },
                new NavItem { Path = "/chats", Role = "ChatsView" },
                new NavItem { Path = "/questions", Role = "ChatsView" },
                new NavItem { Path = "/reviews", Role = "ChatsView" }
            }
        },
        new NavGroup
        {
            Title = "Доска задач",
            Icon = "view_kanban",
            Items = new()
            {
                new NavItem { Path = "/task-board", Role = "TaskBoardView" },
                new NavItem { Path = "/task-board-settings", Role = "TaskBoardAdmin" },
                new NavItem { Path = "/task-board-report", Role = "TaskBoardAdmin" }
            }
        },
        new NavGroup
        {
            Title = "Управление",
            Icon = "settings",
            Items = new()
            {
                new NavItem { Path = "/users", Role = "UsersView" },
                new NavItem { Path = "/audit", Role = "AuditView" },
                new NavItem { Path = "/roles", Role = "ModulesView" },
                new NavItem { Path = "/orders-sync", Role = "OrdersView" },
                new NavItem { Path = "/modules", Role = "ModulesView" }
            }
        }
    };
}
