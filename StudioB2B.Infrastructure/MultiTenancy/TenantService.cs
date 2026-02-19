using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Domain.Entities.Tenants;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Infrastructure.Persistence.Tenant;
using System.Text.RegularExpressions;

namespace StudioB2B.Infrastructure.MultiTenancy;

/// <summary>
/// Сервис управления тенантами
/// </summary>
public partial class TenantService : ITenantService
{
    private readonly MasterDbContext _masterDb;
    private readonly ILogger<TenantService> _logger;
    private readonly MultiTenancyOptions _options;

    public TenantService(
        MasterDbContext masterDb,
        ILogger<TenantService> logger,
        IOptions<MultiTenancyOptions> options)
    {
        _masterDb = masterDb;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken ct = default)
    {
        return await _masterDb.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain, ct);
    }

    public async Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _masterDb.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);
    }

    public async Task<bool> IsSubdomainAvailableAsync(string subdomain, CancellationToken ct = default)
    {
        var normalizedSubdomain = subdomain.ToLowerInvariant().Trim();

        // Проверка зарезервированных субдоменов
        if (_options.ReservedSubdomains.Contains(normalizedSubdomain, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        // Проверка в базе
        return !await _masterDb.Tenants
            .AnyAsync(t => t.Subdomain == normalizedSubdomain, ct);
    }

    public async Task<TenantRegistrationResult> RegisterAsync(
        string companyName,
        string subdomain,
        string adminEmail,
        string adminPassword,
        CancellationToken ct = default)
    {
        try
        {
            var normalizedSubdomain = subdomain.ToLowerInvariant().Trim();

            // Валидация субдомена
            if (!IsValidSubdomain(normalizedSubdomain))
            {
                return new TenantRegistrationResult(false, Error: "Invalid subdomain format. Use only letters, numbers, and hyphens (3-30 chars).");
            }

            // Проверка доступности
            if (!await IsSubdomainAvailableAsync(normalizedSubdomain, ct))
            {
                return new TenantRegistrationResult(false, Error: "Subdomain is already taken or reserved.");
            }

            // Генерация connection string для тенанта
            var dbName = $"StudioB2B_Tenant_{normalizedSubdomain}";
            var connectionString = string.Format(_options.TenantDbConnectionTemplate, dbName);

            // Создание тенанта
            var tenant = Tenant.Create(companyName, normalizedSubdomain, connectionString);

            _masterDb.Tenants.Add(tenant);
            await _masterDb.SaveChangesAsync(ct);

            _logger.LogInformation("Tenant created: {TenantId} ({Subdomain})", tenant.Id, normalizedSubdomain);

            // Создание базы данных тенанта
            await CreateTenantDatabaseAsync(connectionString, ct);

            // Создание администратора в базе тенанта
            await CreateTenantAdminAsync(connectionString, adminEmail, adminPassword, ct);

            _logger.LogInformation("Tenant registration completed: {TenantId}", tenant.Id);

            return new TenantRegistrationResult(true, tenant.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register tenant: {Subdomain}", subdomain);
            return new TenantRegistrationResult(false, Error: "Registration failed. Please try again later.");
        }
    }

    public async Task<bool> SetActiveStateAsync(Guid tenantId, bool isActive, CancellationToken ct = default)
    {
        var tenant = await _masterDb.Tenants.FindAsync([tenantId], ct);
        if (tenant == null) return false;

        if (isActive)
            tenant.Activate();
        else
            tenant.Deactivate();

        await _masterDb.SaveChangesAsync(ct);
        return true;
    }

    private static bool IsValidSubdomain(string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain)) return false;
        if (subdomain.Length < 3 || subdomain.Length > 30) return false;

        // Only letters, numbers, hyphens. Must start/end with letter or number.
        return SubdomainRegex().IsMatch(subdomain);
    }

    [GeneratedRegex(@"^[a-z0-9][a-z0-9\-]*[a-z0-9]$|^[a-z0-9]$")]
    private static partial Regex SubdomainRegex();

    private async Task CreateTenantDatabaseAsync(string connectionString, CancellationToken ct)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

        await using var context = new TenantDbContext(optionsBuilder.Options);

        // Создаём базу и применяем миграции
        await context.Database.MigrateAsync(ct);

        _logger.LogInformation("Tenant database created and migrated");
    }

    private async Task CreateTenantAdminAsync(
        string connectionString,
        string email,
        string password,
        CancellationToken ct)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

        await using var context = new TenantDbContext(optionsBuilder.Options);

        // Загружаем все роли из мастер-базы
        var masterRoles = await _masterDb.Roles.AsNoTracking().ToListAsync(ct);

        // Копируем роли в базу тенанта (если их ещё нет)
        foreach (var masterRole in masterRoles)
        {
            var exists = await context.Roles.AnyAsync(r => r.Id == masterRole.Id, ct);
            if (!exists)
            {
                context.Roles.Add(new ApplicationRole
                {
                    Id = masterRole.Id,
                    Name = masterRole.Name,
                    NormalizedName = masterRole.NormalizedName,
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    Description = masterRole.Description,
                    IsSystemRole = masterRole.IsSystemRole,
                    CreatedAtUtc = masterRole.CreatedAtUtc
                });
            }
        }

        await context.SaveChangesAsync(ct);

        // Создаём UserManager вручную (т.к. мы вне DI scope)
        var userStore = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<ApplicationUser, ApplicationRole, TenantDbContext, Guid>(context);
        var hasher = new PasswordHasher<ApplicationUser>();
        var normalizer = new UpperInvariantLookupNormalizer();

        var validators = new List<IUserValidator<ApplicationUser>>
        {
            new UserValidator<ApplicationUser>()
        };
        var passwordValidators = new List<IPasswordValidator<ApplicationUser>>
        {
            new PasswordValidator<ApplicationUser>()
        };

        using var userManager = new UserManager<ApplicationUser>(
            userStore,
            Options.Create(new IdentityOptions()),
            hasher,
            validators,
            passwordValidators,
            normalizer,
            new IdentityErrorDescriber(),
            null!,
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<UserManager<ApplicationUser>>());

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Admin",
            LastName = "User",
            IsActive = true
        };

        var result = await userManager.CreateAsync(admin, password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create admin user: {errors}");
        }

        // Назначаем пользователю роль Admin
        // Если в мастер-базе нет роли Admin — создаём её в базе тенанта как резервный вариант
        var roleStore = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.RoleStore<ApplicationRole, TenantDbContext, Guid>(context);
        using var roleManager = new RoleManager<ApplicationRole>(
            roleStore,
            Array.Empty<IRoleValidator<ApplicationRole>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<RoleManager<ApplicationRole>>());

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new ApplicationRole
            {
                Name = "Admin",
                Description = "Administrator with full access",
                IsSystemRole = true
            });
        }

        await userManager.AddToRoleAsync(admin, "Admin");

        _logger.LogInformation("Tenant admin user created: {Email}", email);
    }
}
