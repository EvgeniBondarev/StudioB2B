using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StudioB2B.Domain.Entities.Marketplace;
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

        await context.Database.MigrateAsync(ct);
        _logger.LogInformation("Tenant database created and migrated");

        await SeedMarketplaceDataAsync(context, ct);
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
        return new TenantDbContext(builder.Options);
    }

    private static async Task SeedMarketplaceDataAsync(TenantDbContext ctx, CancellationToken ct)
    {
        if (!await ctx.Set<MarketplaceClientType>().AnyAsync(ct))
        {
            ctx.Set<MarketplaceClientType>().AddRange(
                new MarketplaceClientType { Name = "Ozon" },
                new MarketplaceClientType { Name = "Wildberries" },
                new MarketplaceClientType { Name = "Яндекс.Маркет" });
        }

        if (!await ctx.Set<MarketplaceClientMode>().AnyAsync(ct))
        {
            ctx.Set<MarketplaceClientMode>().AddRange(
                new MarketplaceClientMode { Name = "FBS" },
                new MarketplaceClientMode { Name = "FBO" },
                new MarketplaceClientMode { Name = "Express" });
        }

        await ctx.SaveChangesAsync(ct);
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
