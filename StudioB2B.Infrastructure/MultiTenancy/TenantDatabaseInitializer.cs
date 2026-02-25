using Microsoft.EntityFrameworkCore;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Domain.Constants;
using StudioB2B.Domain.Entities.Common;
using StudioB2B.Domain.Entities.Marketplace;
using StudioB2B.Domain.Extensions;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.MultiTenancy;

public class TenantDatabaseInitializer : ITenantDatabaseInitializer
{
    private readonly IPasswordHasher _passwordHasher;

    public TenantDatabaseInitializer(IPasswordHasher passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }

    public async Task MigrateAndSeedAsync(string connectionString, string email, string password, CancellationToken ct)
    {
        await using var context = await CreateContextAsync(connectionString, ct);
        await context.Database.MigrateAsync(ct);
        await SeedDataAsync(context, email, password, ct);
    }

    public async Task DropDatabaseAsync(string connectionString, CancellationToken ct)
    {
        await using var context = await CreateContextAsync(connectionString, ct);
        await context.Database.EnsureDeletedAsync(ct);
    }

    private static async Task<TenantDbContext> CreateContextAsync(string connectionString, CancellationToken ct)
    {
        var builder = new DbContextOptionsBuilder<TenantDbContext>();
        builder.UseMySql(connectionString,
            await ServerVersion.AutoDetectAsync(connectionString, ct));
        return new TenantDbContext(builder.Options);
    }

    private async Task SeedDataAsync(TenantDbContext ctx, string email, string password, CancellationToken ct)
    {
        var marketplaceClientTypes = typeof(MarketplaceClientTypeEnum).ToList<MarketplaceClientType>();
        var marketplaceClientModes = typeof(MarketplaceClientModeEnum).ToList<MarketplaceClientMode>();
        var roles = typeof(RoleEnum).ToList<Role>();

        var users = new List<User>
                    {
                        new()
                        {
                            Email = email,
                            PasswordHash = _passwordHasher.Hash(password),
                            Roles = roles
                        }
                    };

        if (!await ctx.Set<MarketplaceClientType>().AnyAsync(ct))
            ctx.Set<MarketplaceClientType>().AddRange(marketplaceClientTypes);

        if (!await ctx.Set<MarketplaceClientMode>().AnyAsync(ct))
            ctx.Set<MarketplaceClientMode>().AddRange(marketplaceClientModes);

        if (!await ctx.Set<Role>().AnyAsync(ct))
            ctx.Set<Role>().AddRange(roles);

        if (!await ctx.Set<User>().AnyAsync(ct))
            ctx.Set<User>().AddRange(users);

        await ctx.SaveChangesAsync(ct);
    }
}
