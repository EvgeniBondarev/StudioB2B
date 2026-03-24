namespace StudioB2B.Web.Services;

public class NavItem
{
    public string Path { get; set; } = string.Empty;
    public string? Role { get; set; } // Роль, необходимая для доступа к пункту
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
            Items = new()
            {
                new NavItem { Path = "/" } // Главная страница, доступна всем
            }
        },
        new NavGroup
        {
            Title = "Пользователи",
            Icon = "people",
            Items = new()
            {
                new NavItem { Path = "/users", Role = "UsersView" }
            }
        },
        new NavGroup
        {
            Title = "Menu-1",
            Icon = "shopping_cart",
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
            Title = "Menu-2",
            Icon = "inventory",
            Items = new()
            {
                new NavItem { Path = "/marketplace-clients", Role = "MarketplaceClientsView" },
                new NavItem { Path = "/chats", Role = "ChatsView" },
                new NavItem { Path = "/questions", Role = "QuestionsView" },
                new NavItem { Path = "/reviews", Role = "ReviewsView" },
                new NavItem { Path = "/price-types", Role = "PriceTypesView" },
                new NavItem { Path = "/calculation-rules", Role = "CalculationRulesView" },
                new NavItem { Path = "/order-statuses", Role = "OrderStatusesView" }
            }
        },
        new NavGroup
        {
            Title = "Menu-3",
            Icon = "settings",
            Items = new()
            {
                new NavItem { Path = "/audit", Role = "AuditView" },
                new NavItem { Path = "/roles", Role = "ModulesView" },
                new NavItem { Path = "/orders-sync", Role = "OrdersView" }
            }
        }
    };
}
