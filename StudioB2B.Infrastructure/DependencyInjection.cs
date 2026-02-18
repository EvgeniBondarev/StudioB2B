using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Infrastructure.Persistence.Master;
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

        return services;
    }
}
