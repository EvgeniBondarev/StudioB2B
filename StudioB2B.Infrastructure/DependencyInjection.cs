using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Infrastructure.Services;
using System.Text;
using StudioB2B.Domain.Options;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Infrastructure.Helpers.Http.Handlers;
using StudioB2B.Infrastructure.Authorization;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Services.MultiTenancy;
using StudioB2B.Infrastructure.Services.Modules;
using StudioB2B.Infrastructure.Services.Order;
using StudioB2B.Infrastructure.Services.Ozon;using TenantService = StudioB2B.Infrastructure.Services.MultiTenancy.TenantService;

namespace StudioB2B.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MultiTenancyOptions>(
            configuration.GetSection(MultiTenancyOptions.SectionName));

        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(DependencyInjection).Assembly));

        services.AddDbContext<MasterDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("MasterDb");
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        });

        services.AddHostedService<DatabaseMigrationService>();

        services.AddSingleton<IKeyEncryptionService, KeyEncryptionService>();

        services.AddTransient<LoggingHandler>();
        services.AddTransient<RetryHandler>();
        services.AddTransient<RateLimitHandler>();

        var ozonSection = configuration.GetSection("Ozon");
        var ozonBaseAddress = ozonSection.GetValue<string>("BaseAddress") ?? "https://api-seller.ozon.ru/";
        var ozonTimeoutSeconds = ozonSection.GetValue<int?>("TimeoutSeconds") ?? 30;

        services.AddHttpClient("Ozon", client =>
        {
            client.BaseAddress = new Uri(ozonBaseAddress);
            client.Timeout = TimeSpan.FromSeconds(ozonTimeoutSeconds);
        })
        .AddHttpMessageHandler<LoggingHandler>()
        .AddHttpMessageHandler<RetryHandler>()
        .AddHttpMessageHandler<RateLimitHandler>();

        services.AddScoped<IOzonApiClient, OzonApiClient>();
        services.AddScoped<IOrderAdapter, OzonFbsOrderAdapter>();
        services.AddScoped<IOrderAdapter, OzonFboOrderAdapter>();
        services.AddScoped<IOrderSyncService, OrderSyncService>();
        services.AddScoped<IOzonChatService, OzonChatService>();
        services.AddScoped<IOzonQuestionsService, OzonQuestionsService>();
        services.AddScoped<IOzonReviewsService, OzonReviewsService>();

        services.AddScoped<TenantProvider>();
        services.AddScoped<ITenantProvider>(sp => sp.GetRequiredService<TenantProvider>());

        services.AddSingleton<ISubdomainResolver, SubdomainResolver>();
        services.AddScoped<ITenantDatabaseInitializer, TenantDatabaseInitializer>();
        services.AddScoped<UserContext>();
        services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();
        services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<CircuitHandler, TenantCircuitHandler>();

        services.AddScoped<MasterAuthService>();

        // Tenant DbContext (Scoped, dynamic connection)
        services.AddScoped(sp =>
        {
            var tenantProvider = sp.GetRequiredService<ITenantProvider>();

            if (!tenantProvider.IsResolved)
                throw new InvalidOperationException("Tenant is not resolved. Ensure TenantMiddleware is configured.");

            var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            optionsBuilder.UseMySql(
                tenantProvider.ConnectionString!,
                ServerVersion.AutoDetect(tenantProvider.ConnectionString!));

            var currentUserProvider = sp.GetService<ICurrentUserProvider>();
            return new TenantDbContext(optionsBuilder.Options, currentUserProvider);
        });

        // Factory — для сервисов, которым нужно создавать независимые контексты
        // (например OrderSyncJobService, вызываемый параллельно из polling + UI)
        services.AddScoped<ITenantDbContextCreator, TenantDbContextCreator>();

        var jwtSection = configuration.GetSection("Jwt");
        var secret = jwtSection["Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured");
        var issuer = jwtSection["Issuer"] ?? "StudioB2B";
        var audience = jwtSection["Audience"] ?? "StudioB2B";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
        services.AddSingleton<IAuthorizationHandler, AdminSatisfiesAllRolesHandler>();

        // Feature operations moved to TenantDbContext extension methods (e.g. db.GetUsersAsync(...)).
        // No scoped registrations required for these extension methods.

        services.AddScoped<IEntityFilterService, EntityFilterService>();

        services.AddSingleton<TenantHangfireManager>();
        services.AddHostedService(sp => sp.GetRequiredService<TenantHangfireManager>());

        services.AddScoped<IOrderSyncJobService, OrderSyncJobService>();

        services.AddScoped<CalculationEngine>();
        services.AddScoped<IOrderTransactionService, OrderTransactionService>();

        services.AddScoped<IModuleService, ModuleService>();
        services.AddScoped<IModuleActivator, ManufacturerModuleActivator>();

        return services;
    }
}
