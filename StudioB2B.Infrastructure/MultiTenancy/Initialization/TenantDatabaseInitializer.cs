using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StudioB2B.Domain.Entities.Marketplace;
using StudioB2B.Domain.Entities.Orders;
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

        await SeedMarketplaceDataAsync(context, ct);
        await EnsureRobotUserAsync(context, ct);
    }

    public async Task CreateAdminUserAsync(
        string connectionString, string email, string password, CancellationToken ct)
    {
        await using var context = await CreateContextAsync(connectionString, ct);

        await SyncRolesFromMasterAsync(context, ct);
        await EnsureAdminRoleAsync(context);
        await CreateUserWithRoleAsync(context, email, password, "Admin");

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
        // ICurrentUserProvider = null → все изменения при инициализации записываются на робота
        return new TenantDbContext(builder.Options, currentUserProvider: null);
    }

    private static async Task EnsureRobotUserAsync(TenantDbContext ctx, CancellationToken ct)
    {
        if (await ctx.Users.AnyAsync(u => u.Id == SystemUser.RobotId, ct))
            return;

        var store = new UserStore<ApplicationUser, ApplicationRole, TenantDbContext, Guid>(ctx);

        using var mgr = new UserManager<ApplicationUser>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            new[] { new UserValidator<ApplicationUser>() },
            new[] { new PasswordValidator<ApplicationUser>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);

        var robot = new ApplicationUser
        {
            Id             = SystemUser.RobotId,
            UserName       = SystemUser.RobotUserName,
            Email          = SystemUser.RobotEmail,
            EmailConfirmed = true,
            FirstName      = SystemUser.RobotFirstName,
            LastName       = SystemUser.RobotLastName,
            IsActive       = false
        };

        // Создаём без пароля — робот не должен иметь возможности войти
        var result = await mgr.CreateAsync(robot);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create robot user: {errors}");
        }
    }

    private static async Task SeedMarketplaceDataAsync(TenantDbContext ctx, CancellationToken ct)
    {
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

        // Системные статусы заказов (OrderStatus)
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

        // Статусы отправлений Ozon (отображаемое имя — на русском, синоним — код API; не системные, привязаны к типу Ozon)
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
                        Name = name,
                        Synonym = synonym,
                        IsInternal = false,
                        IsTerminal = isTerminal,
                        MarketplaceClientTypeId = ozonType.Id
                    });
                }
                else
                {
                    // Обновляем существующий статус, если он был создан ранее с английским именем или неверными флагами
                    var needsUpdate = false;

                    if (existing.Name != name)
                    {
                        existing.Name = name;
                        needsUpdate = true;
                    }

                    if (existing.IsInternal)
                    {
                        existing.IsInternal = false;
                        needsUpdate = true;
                    }

                    if (existing.MarketplaceClientTypeId != ozonType.Id)
                    {
                        existing.MarketplaceClientTypeId = ozonType.Id;
                        needsUpdate = true;
                    }

                    if (existing.IsTerminal != isTerminal)
                    {
                        existing.IsTerminal = isTerminal;
                        needsUpdate = true;
                    }

                    if (existing.IsDeleted)
                    {
                        existing.IsDeleted = false;
                        needsUpdate = true;
                    }

                    if (needsUpdate)
                    {
                        ctx.Set<OrderStatus>().Update(existing);
                    }
                }
            }
            await ctx.SaveChangesAsync(ct);
        }

        // Цвета статусов (StatusColor)
        if (!await ctx.Set<StatusColor>().AnyAsync(ct))
        {
            var statuses = await ctx.Set<OrderStatus>()
                .AsNoTracking()
                .ToListAsync(ct);

            StatusColor CreateIfExists(string statusName, string colorHex)
            {
                var status = statuses.FirstOrDefault(s => s.Name == statusName);
                return status is null
                    ? null!
                    : new StatusColor { OrderStatusId = status.Id, Hash = colorHex };
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
            }
            .Where(c => c is not null)
            .ToList()!;

            if (colors.Count > 0)
            {
                ctx.Set<StatusColor>().AddRange(colors);
                await ctx.SaveChangesAsync(ct);
            }
        }
    }

    private async Task SyncRolesFromMasterAsync(TenantDbContext ctx, CancellationToken ct)
    {
        var masterRoles = await _masterDb.Roles.AsNoTracking().ToListAsync(ct);

        foreach (var mr in masterRoles)
        {
            if (!await ctx.Roles.AnyAsync(r => r.Id == mr.Id, ct))
            {
                ctx.Roles.Add(new ApplicationRole
                {
                    Id = mr.Id,
                    Name = mr.Name,
                    NormalizedName = mr.NormalizedName,
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    Description = mr.Description,
                    IsSystemRole = mr.IsSystemRole,
                    CreatedAtUtc = mr.CreatedAtUtc
                });
            }
        }

        await ctx.SaveChangesAsync(ct);
    }

    private static async Task EnsureAdminRoleAsync(TenantDbContext ctx)
    {
        var store = new RoleStore<ApplicationRole, TenantDbContext, Guid>(ctx);
        using var mgr = new RoleManager<ApplicationRole>(
            store,
            Array.Empty<IRoleValidator<ApplicationRole>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            NullLogger<RoleManager<ApplicationRole>>.Instance);

        if (!await mgr.RoleExistsAsync("Admin"))
        {
            await mgr.CreateAsync(new ApplicationRole
            {
                Name = "Admin",
                Description = "Administrator with full access",
                IsSystemRole = true
            });
        }
    }

    private static async Task CreateUserWithRoleAsync(
        TenantDbContext ctx, string email, string password, string role)
    {
        var store = new UserStore<ApplicationUser, ApplicationRole, TenantDbContext, Guid>(ctx);

        using var mgr = new UserManager<ApplicationUser>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            new[] { new UserValidator<ApplicationUser>() },
            new[] { new PasswordValidator<ApplicationUser>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Admin",
            LastName = "User",
            IsActive = true
        };

        var result = await mgr.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create admin user: {errors}");
        }

        await mgr.AddToRoleAsync(user, role);
    }
}
