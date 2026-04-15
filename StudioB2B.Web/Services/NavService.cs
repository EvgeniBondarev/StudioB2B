using StudioB2B.Domain.Constants;

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
                new NavItem { Path = "/orders", Role = nameof(PageEnum.OrdersView) },
                new NavItem { Path = "/returns", Role = nameof(PageEnum.ReturnsView) },
                new NavItem { Path = "/transactions", Role = nameof(PageEnum.TransactionsView) },
                new NavItem { Path = "/orders/apply-transaction", Role = nameof(PageEnum.TransactionsView) },
                new NavItem { Path = "/transactions-history", Role = nameof(PageEnum.TransactionsView) }
            }
        },
        new NavGroup
        {
            Title = "Маркетплейс",
            Icon = "storefront",
            Items = new()
            {
                new NavItem { Path = "/marketplace-clients", Role = nameof(PageEnum.MarketplaceClientsView) },
                new NavItem { Path = "/price-types", Role = nameof(PageEnum.PriceTypesView) },
                new NavItem { Path = "/calculation-rules", Role = nameof(PageEnum.CalculationRulesView) },
                new NavItem { Path = "/order-statuses", Role = nameof(PageEnum.OrderStatusesView) },
                new NavItem { Path = "/ozon-push", Role = nameof(PageEnum.OzonPushView) }
            }
        },
        new NavGroup
        {
            Title = "Коммуникации",
            Icon = "forum",
            Items = new()
            {
                new NavItem { Path = "/communication", Role = nameof(PageEnum.ChatsView) }
            }
        },
        new NavGroup
        {
            Title = "Доска задач",
            Icon = "view_kanban",
            Items = new()
            {
                new NavItem { Path = "/task-board", Role = nameof(PageEnum.TaskBoardView) },
                new NavItem { Path = "/task-board-settings", Role = nameof(PageEnum.TaskBoardAdmin) },
                new NavItem { Path = "/task-board-report", Role = nameof(PageEnum.TaskBoardAdmin) }
            }
        },
        new NavGroup
        {
            Title = "Управление",
            Icon = "settings",
            Items = new()
            {
                new NavItem { Path = "/users", Role = nameof(PageEnum.UsersView) },
                new NavItem { Path = "/audit", Role = nameof(PageEnum.AuditView) },
                new NavItem { Path = "/permissions", Role = nameof(PageEnum.PermissionsView) },
                new NavItem { Path = "/orders-sync", Role = nameof(PageEnum.OrdersView) },
                new NavItem { Path = "/modules", Role = nameof(PageEnum.ModulesView) }
            }
        }
    };
}
