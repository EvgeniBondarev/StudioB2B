using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Infrastructure.MultiTenancy;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Infrastructure.Services;

namespace StudioB2B.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database Options
        services.Configure<DatabaseOptions>(
            configuration.GetSection(DatabaseOptions.SectionName));

        // Multi-Tenancy Options
        services.Configure<MultiTenancyOptions>(
            configuration.GetSection(MultiTenancyOptions.SectionName));

        // Master DbContext (MySQL)
        services.AddDbContext<MasterDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("MasterDb");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("MasterDb connection string is not configured.");
            }

            // MySQL с автоопределением версии
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        });

        // Database migration service (runs on startup)
        services.AddHostedService<DatabaseMigrationService>();

        // Tenant Provider (Scoped - per request)
        services.AddScoped<TenantProvider>();
        services.AddScoped<ITenantProvider>(sp => sp.GetRequiredService<TenantProvider>());

        // Current User Provider
        services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();

        // Tenant DbContext Factory
        services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();

        // Tenant Service
        services.AddScoped<ITenantService, TenantService>();

        // Blazor Circuit Handler для заполнения TenantProvider
        services.AddScoped<CircuitHandler, TenantCircuitHandler>();

        // Tenant DbContext (Scoped, dynamic connection)
        services.AddScoped(sp =>
        {
            var tenantProvider = sp.GetRequiredService<ITenantProvider>();
            var currentUser = sp.GetRequiredService<ICurrentUserProvider>();

            if (!tenantProvider.IsResolved)
            {
                throw new InvalidOperationException("Tenant is not resolved. Ensure TenantMiddleware is configured.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            optionsBuilder.UseMySql(
                tenantProvider.ConnectionString!,
                ServerVersion.AutoDetect(tenantProvider.ConnectionString!));

            return new TenantDbContext(optionsBuilder.Options, currentUser.UserId);
        });

        // ASP.NET Identity (using Tenant DbContext)
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;

                // User settings
                options.User.RequireUniqueEmail = true;

                // SignIn settings
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<TenantDbContext>()
            .AddDefaultTokenProviders();

        // Cookie configuration
        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/login";
            options.LogoutPath = "/logout";
            options.AccessDeniedPath = "/access-denied";
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.SlidingExpiration = true;
        });

        return services;
    }
}
