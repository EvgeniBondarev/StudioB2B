using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioB2B.Domain.Entities.Marketplace;
using StudioB2B.Domain.Entities.Orders;
using StudioB2B.Domain.Entities.Tenants;
using StudioB2B.Domain.Entities.References;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.MultiTenancy.Initialization;

public class TenantDatabaseInitializer : ITenantDatabaseInitializer
{
    private readonly MasterDbContext _masterDb;
    private readonly ILogger<TenantDatabaseInitializer> _logger;

    public TenantDatabaseInitializer(
        MasterDbContext masterDb,
        ILogger<TenantDatabaseInitializer> logger)
    {
        _masterDb = masterDb;
        _logger = logger;
    }

    public async Task MigrateAndSeedAsync(string connectionString, CancellationToken ct)
    {
        await using var context = await CreateContextAsync(connectionString, ct);

        var pending = await context.Database.GetPendingMigrationsAsync(ct);
        var pendingList = pending.ToList();

        if (pendingList.Count > 0)
        {
            _logger.LogInformation("Applying {Count} pending tenant migrations: {Migrations}",
                pendingList.Count, string.Join(", ", pendingList));

            await context.Database.MigrateAsync(ct);

            _logger.LogInformation("Tenant database created and migrated");
        }
        else
        {
            _logger.LogInformation("No pending tenant migrations, database is up to date");
        }

        await RunSeedAsync(context, ct);
    }

    public async Task MigrateOnlyAsync(string connectionString, CancellationToken ct)
    {
        await using var context = await CreateContextAsync(connectionString, ct);

        var pending = (await context.Database.GetPendingMigrationsAsync(ct)).ToList();
        if (pending.Count > 0)
        {
            _logger.LogInformation(
                "Startup: applying {Count} pending tenant migrations: {Migrations}",
                pending.Count, string.Join(", ", pending));

            await context.Database.MigrateAsync(ct);

            _logger.LogInformation("Startup: tenant database migrated successfully");
        }

        await RunSeedAsync(context, ct);
    }

    private async Task RunSeedAsync(TenantDbContext context, CancellationToken ct)
    {
        await SeedMarketplaceDataAsync(context, ct);
        await SeedBasePriceTypesAsync(context, ct);
        await SeedBaseCalculationRulesAsync(context, ct);
        await SeedBaseOrderTransactionsAsync(context, ct);
        await EnsureRobotUserAsync(context, ct);
    }

    public async Task CreateAdminUserAsync(
        string connectionString, string email, string password, CancellationToken ct)
    {
        await using var context = await CreateContextAsync(connectionString, ct);

        await SyncRolesFromMasterAsync(context, ct);
        await EnsureAdminRoleAsync(context, ct);
        await CreateUserWithRoleAsync(context, email, password, "Admin", ct);

        _logger.LogInformation("Tenant admin user created: {Email}", email);
    }

    public async Task DropDatabaseAsync(string connectionString, CancellationToken ct)
    {
        await using var context = await CreateContextAsync(connectionString, ct);
        await context.Database.EnsureDeletedAsync(ct);
        _logger.LogInformation("Tenant database dropped");
    }

    private static async Task<TenantDbContext> CreateContextAsync(
        string connectionString, CancellationToken ct)
    {
        var builder = new DbContextOptionsBuilder<TenantDbContext>();
        builder.UseMySql(connectionString,
            await ServerVersion.AutoDetectAsync(connectionString, ct));
        return new TenantDbContext(builder.Options, currentUserProvider: null);
    }

    private static async Task EnsureRobotUserAsync(TenantDbContext ctx, CancellationToken ct)
    {
        if (await ctx.Users.AnyAsync(u => u.Id == SystemUser.RobotId, ct))
            return;

        ctx.Users.Add(new User
        {
            Id           = SystemUser.RobotId,
            Email        = SystemUser.RobotEmail,
            HashPassword = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // недоступный пароль
            FirstName    = SystemUser.RobotFirstName,
            LastName     = SystemUser.RobotLastName,
            IsActive     = false
        });

        await ctx.SaveChangesAsync(ct);
    }

    private async Task SyncRolesFromMasterAsync(TenantDbContext ctx, CancellationToken ct)
    {
        var masterRoles = await _masterDb.Roles.AsNoTracking().ToListAsync(ct);

        foreach (var mr in masterRoles)
        {
            if (!await ctx.Roles.AnyAsync(r => r.Id == mr.Id, ct))
            {
                ctx.Roles.Add(new Role { Id = mr.Id, Name = mr.Name });
            }
        }

        await ctx.SaveChangesAsync(ct);
    }

    private static async Task EnsureAdminRoleAsync(TenantDbContext ctx, CancellationToken ct)
    {
        if (!await ctx.Roles.AnyAsync(r => r.Name == "Admin", ct))
        {
            ctx.Roles.Add(new Role { Id = Guid.NewGuid(), Name = "Admin" });
            await ctx.SaveChangesAsync(ct);
        }
    }

    private static async Task CreateUserWithRoleAsync(
        TenantDbContext ctx, string email, string password, string roleName, CancellationToken ct)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (await ctx.Users.AnyAsync(u => u.Email == normalizedEmail, ct))
            return;

        var user = new User
        {
            Id           = Guid.NewGuid(),
            Email        = normalizedEmail,
            HashPassword = BCrypt.Net.BCrypt.HashPassword(password),
            FirstName    = "Admin",
            LastName     = "User",
            IsActive     = true
        };

        ctx.Users.Add(user);

        var role = await ctx.Roles.FirstOrDefaultAsync(r => r.Name == roleName, ct);
        if (role is not null)
            ctx.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });

        await ctx.SaveChangesAsync(ct);
    }

    private static async Task SeedMarketplaceDataAsync(TenantDbContext ctx, CancellationToken ct)
    {
        // Валюта RUB
        if (!await ctx.Set<Currency>().AnyAsync(c => c.Code == "RUB", ct))
        {
            ctx.Set<Currency>().Add(new Currency { Code = "RUB", Name = "Российский рубль" });
            await ctx.SaveChangesAsync(ct);
        }

        // Типы клиентов маркетплейсов
        if (!await ctx.Set<MarketplaceClientType>().AnyAsync(ct))
        {
            ctx.Set<MarketplaceClientType>().AddRange(
                new MarketplaceClientType { Name = "Ozon" },
                new MarketplaceClientType { Name = "Wildberries" },
                new MarketplaceClientType { Name = "Яндекс.Маркет" });
        }

        // Режимы клиентов маркетплейсов
        if (!await ctx.Set<MarketplaceClientMode>().AnyAsync(ct))
        {
            ctx.Set<MarketplaceClientMode>().AddRange(
                new MarketplaceClientMode { Name = "FBS" },
                new MarketplaceClientMode { Name = "FBO" },
                new MarketplaceClientMode { Name = "Express" });
        }

        // Системные статусы заказов
        if (!await ctx.Set<OrderStatus>().AnyAsync(ct))
        {
            var systemStatuses = new List<OrderStatus>
            {
                new() { Name = "Не указан",            Color = "#9E9E9E", IsTerminal = false, IsInternal = true },
                new() { Name = "Готов к отгрузке",     Color = "#4CAF50", IsTerminal = false, IsInternal = true },
                new() { Name = "Не готов",             Color = "#FF9800", IsTerminal = false, IsInternal = true },
                new() { Name = "Заказан поставщику",   Color = "#2196F3", IsTerminal = false, IsInternal = true },
                new() { Name = "Изменен",              Color = "#03A9F4", IsTerminal = false, IsInternal = true },
                new() { Name = "Отменен",              Color = "#F44336", IsTerminal = true,  IsInternal = true },
                new() { Name = "Возврат покупателя",   Color = "#9C27B0", IsTerminal = true,  IsInternal = true },
                new() { Name = "Доставлен",            Color = "#4CAF50", IsTerminal = true,  IsInternal = true },
                new() { Name = "Отгружен клиенту",     Color = "#4CAF50", IsTerminal = true,  IsInternal = true },
                new() { Name = "Отгружен поставщиком", Color = "#4CAF50", IsTerminal = false, IsInternal = true },
                new() { Name = "Возврат поставщику",   Color = "#9C27B0", IsTerminal = false, IsInternal = true },
                new() { Name = "Возвращено поставщику",Color = "#9C27B0", IsTerminal = true,  IsInternal = true },
                new() { Name = "Приостановлен",        Color = "#FFC107", IsTerminal = false, IsInternal = true },
                new() { Name = "ПринятНаСкладе",       Color = "#4CAF50", IsTerminal = false, IsInternal = true },
                new() { Name = "К отмене",             Color = "#FF5722", IsTerminal = false, IsInternal = true },
                new() { Name = "Отгружен реализатором",Color = "#4CAF50", IsTerminal = false, IsInternal = true },
                new() { Name = "Утерян реализатором",  Color = "#607D8B", IsTerminal = true,  IsInternal = true },
                new() { Name = "Заказан реализатору",  Color = "#2196F3", IsTerminal = false, IsInternal = true },
                new() { Name = "Утерян поставщиком",   Color = "#607D8B", IsTerminal = true,  IsInternal = true },
                new() { Name = "Отгружен реализатору", Color = "#4CAF50", IsTerminal = false, IsInternal = true },
                new() { Name = "Перемещение",          Color = "#3F51B5", IsTerminal = false, IsInternal = true },
                new() { Name = "Возвращен на склад",   Color = "#009688", IsTerminal = false, IsInternal = true }
            };
            ctx.Set<OrderStatus>().AddRange(systemStatuses);
        }

        await ctx.SaveChangesAsync(ct);

        // Статусы отправлений Ozon
        var ozonType = await ctx.Set<MarketplaceClientType>()
            .FirstOrDefaultAsync(t => t.Name == "Ozon", ct);
        if (ozonType != null)
        {
            var ozonShipmentStatuses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["acceptance_in_progress"] = "идёт приёмка",
                ["arbitration"] = "арбитраж",
                ["awaiting_approve"] = "ожидает подтверждения",
                ["awaiting_deliver"] = "ожидает отгрузки",
                ["awaiting_packaging"] = "ожидает упаковки",
                ["awaiting_registration"] = "ожидает регистрации",
                ["awaiting_verification"] = "создано",
                ["cancelled"] = "отменено",
                ["cancelled_from_split_pending"] = "отменён из-за разделения отправления",
                ["client_arbitration"] = "клиентский арбитраж доставки",
                ["delivering"] = "доставляется",
                ["driver_pickup"] = "у водителя",
                ["not_accepted"] = "не принят на сортировочном центре",
                ["delivered"] = "доставлено"
            };
            foreach (var (synonym, name) in ozonShipmentStatuses)
            {
                var existing = await ctx.Set<OrderStatus>()
                    .FirstOrDefaultAsync(s => s.Synonym == synonym, ct);
                var isTerminal = string.Equals(synonym, "delivered", StringComparison.OrdinalIgnoreCase);
                if (existing == null)
                {
                    ctx.Set<OrderStatus>().Add(new OrderStatus
                    {
                        Name = name, Synonym = synonym,
                        IsInternal = false, IsTerminal = isTerminal,
                        MarketplaceClientTypeId = ozonType.Id
                    });
                }
                else
                {
                    var needsUpdate = false;
                    if (existing.Name != name) { existing.Name = name; needsUpdate = true; }
                    if (existing.IsInternal) { existing.IsInternal = false; needsUpdate = true; }
                    if (existing.MarketplaceClientTypeId != ozonType.Id) { existing.MarketplaceClientTypeId = ozonType.Id; needsUpdate = true; }
                    if (existing.IsTerminal != isTerminal) { existing.IsTerminal = isTerminal; needsUpdate = true; }
                    if (existing.IsDeleted) { existing.IsDeleted = false; needsUpdate = true; }
                    if (needsUpdate) ctx.Set<OrderStatus>().Update(existing);
                }
            }
            await ctx.SaveChangesAsync(ct);
        }

        // Цвета статусов
        if (!await ctx.Set<StatusColor>().AnyAsync(ct))
        {
            var statuses = await ctx.Set<OrderStatus>().AsNoTracking().ToListAsync(ct);
            StatusColor? CreateIfExists(string statusName, string colorHex)
            {
                var status = statuses.FirstOrDefault(s => s.Name == statusName);
                return status is null ? null : new StatusColor { OrderStatusId = status.Id, Hash = colorHex };
            }
            var colors = new List<StatusColor?>
            {
                CreateIfExists("Не указан", "#9E9E9E"),
                CreateIfExists("Готов к отгрузке", "#4CAF50"),
                CreateIfExists("Не готов", "#FF9800"),
                CreateIfExists("Отменен", "#F44336"),
                CreateIfExists("Доставлен", "#4CAF50"),
                CreateIfExists("Приостановлен", "#FFC107"),
                CreateIfExists("Возврат покупателя", "#9C27B0"),
                CreateIfExists("Возвращен на склад", "#009688")
            }.Where(c => c is not null).ToList()!;
            if (colors.Count > 0)
            {
                ctx.Set<StatusColor>().AddRange(colors);
                await ctx.SaveChangesAsync(ct);
            }
        }
    }

    private static async Task SeedBasePriceTypesAsync(TenantDbContext ctx, CancellationToken ct)
    {
        var basePriceTypeNames = new[] { "Цена", "Цена до скидки", "Себестоимость", "Скидка", "Маржа" };
        foreach (var name in basePriceTypeNames)
        {
            if (!await ctx.Set<PriceType>().AnyAsync(pt => pt.Name == name, ct))
            {
                ctx.Set<PriceType>().Add(new PriceType
                {
                    Name = name,
                    IsUserDefined = true
                });
            }
        }
        await ctx.SaveChangesAsync(ct);
    }

    private static async Task SeedBaseCalculationRulesAsync(TenantDbContext ctx, CancellationToken ct)
    {
        if (!await ctx.Set<CalculationRule>().AnyAsync(r => r.ResultKey == "Скидка", ct))
        {
            ctx.Set<CalculationRule>().Add(new CalculationRule
            {
                Name = "Скидка",
                ResultKey = "Скидка",
                Formula = "ЦенаДоСкидки - Цена",
                SortOrder = 10,
                IsActive = true
            });
        }
        if (!await ctx.Set<CalculationRule>().AnyAsync(r => r.ResultKey == "Маржа", ct))
        {
            ctx.Set<CalculationRule>().Add(new CalculationRule
            {
                Name = "Маржа",
                ResultKey = "Маржа",
                Formula = "Цена - Себестоимость",
                SortOrder = 20,
                IsActive = true
            });
        }
        await ctx.SaveChangesAsync(ct);
    }

    private static async Task SeedBaseOrderTransactionsAsync(TenantDbContext ctx, CancellationToken ct)
    {
        var statuses = await ctx.Set<OrderStatus>()
            .Where(s => s.IsInternal && !s.IsDeleted)
            .AsNoTracking()
            .ToListAsync(ct);

        var priceType = await ctx.Set<PriceType>()
            .FirstOrDefaultAsync(pt => pt.Name == "Цена" && !pt.IsDeleted, ct);

        Guid? GetStatusId(string name) =>
            statuses.FirstOrDefault(s => s.Name == name)?.Id;

        // В работу: Не указан → Не готов (без правил)
        if (!await ctx.Set<OrderTransaction>().AnyAsync(t => t.Name == "В работу" && !t.IsDeleted, ct))
        {
            var fromId = GetStatusId("Не указан");
            var toId = GetStatusId("Не готов");
            if (fromId.HasValue && toId.HasValue)
            {
                ctx.Set<OrderTransaction>().Add(new OrderTransaction
                {
                    Name = "В работу",
                    FromSystemStatusId = fromId.Value,
                    ToSystemStatusId = toId.Value,
                    SortOrder = 0,
                    IsEnabled = true
                });
            }
        }

        // Готов к отгрузке: Не готов → Готов к отгрузке (правило: Цена = Цена)
        if (!await ctx.Set<OrderTransaction>().AnyAsync(t => t.Name == "Готов к отгрузке" && !t.IsDeleted, ct))
        {
            var fromId = GetStatusId("Не готов");
            var toId = GetStatusId("Готов к отгрузке");
            if (fromId.HasValue && toId.HasValue && priceType != null)
            {
                var txn = new OrderTransaction
                {
                    Name = "Готов к отгрузке",
                    FromSystemStatusId = fromId.Value,
                    ToSystemStatusId = toId.Value,
                    SortOrder = 10,
                    IsEnabled = true
                };
                ctx.Set<OrderTransaction>().Add(txn);
                await ctx.SaveChangesAsync(ct);

                ctx.Set<OrderTransactionRule>().Add(new OrderTransactionRule
                {
                    OrderTransactionId = txn.Id,
                    PriceTypeId = priceType.Id,
                    ValueSource = TransactionValueSource.Formula,
                    Formula = "Цена",
                    SortOrder = 0
                });
            }
        }

        // Отгружен: Готов к отгрузке → Отгружен клиенту (правило: Цена = Цена)
        if (!await ctx.Set<OrderTransaction>().AnyAsync(t => t.Name == "Отгружен" && !t.IsDeleted, ct))
        {
            var fromId = GetStatusId("Готов к отгрузке");
            var toId = GetStatusId("Отгружен клиенту");
            if (fromId.HasValue && toId.HasValue && priceType != null)
            {
                var txn = new OrderTransaction
                {
                    Name = "Отгружен",
                    FromSystemStatusId = fromId.Value,
                    ToSystemStatusId = toId.Value,
                    SortOrder = 20,
                    IsEnabled = true
                };
                ctx.Set<OrderTransaction>().Add(txn);
                await ctx.SaveChangesAsync(ct);

                ctx.Set<OrderTransactionRule>().Add(new OrderTransactionRule
                {
                    OrderTransactionId = txn.Id,
                    PriceTypeId = priceType.Id,
                    ValueSource = TransactionValueSource.Formula,
                    Formula = "Цена",
                    SortOrder = 0
                });
            }
        }

        await ctx.SaveChangesAsync(ct);
    }
}
