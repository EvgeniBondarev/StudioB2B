using Microsoft.EntityFrameworkCore;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Domain.Constants;
using StudioB2B.Domain.Entities.Common;
using StudioB2B.Domain.Entities.Tenant;
using StudioB2B.Domain.Entities.Tenant.Marketplace;
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

    public async Task MigrateAndSeedAsync(string connectionString, string email, string password,
        string surname, string firstName, string patronymic, CancellationToken ct)
    {
        await using var context = await CreateContextAsync(connectionString, ct);
        await context.Database.MigrateAsync(ct);
        await SeedDataAsync(context, email, password, surname, firstName, patronymic, ct);
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

    private async Task SeedDataAsync(TenantDbContext ctx, string email, string password,
        string surname, string firstName, string patronymic, CancellationToken ct)
    {
        var marketplaceClientTypes = typeof(MarketplaceClientTypeEnum).ToList<MarketplaceClientType>();
        var marketplaceClientModes = typeof(MarketplaceClientModeEnum).ToList<MarketplaceClientMode>();
        var roles = typeof(RoleEnum).ToList<TenantRole>();

        var users = new List<TenantUser>
        {
            new()
            {
                Email = email,
                Surname = surname,
                FirstName = firstName,
                Patronymic = patronymic,
                PasswordHash = _passwordHasher.Hash(password),
                Roles = roles
            }
        };

        if (!await ctx.Set<MarketplaceClientType>().AnyAsync(ct))
            ctx.Set<MarketplaceClientType>().AddRange(marketplaceClientTypes);

        if (!await ctx.Set<MarketplaceClientMode>().AnyAsync(ct))
            ctx.Set<MarketplaceClientMode>().AddRange(marketplaceClientModes);

        if (!await ctx.Set<TenantRole>().AnyAsync(ct))
            ctx.Set<TenantRole>().AddRange(roles);

        if (!await ctx.Set<TenantUser>().AnyAsync(ct))
            ctx.Set<TenantUser>().AddRange(users);

        await ctx.SaveChangesAsync(ct);
    }
}
