using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Infrastructure.Features.Marketplace;
using StudioB2B.Infrastructure.Features.Orders;
using StudioB2B.Infrastructure.Features.Roles;
using StudioB2B.Infrastructure.Features.Users;
using StudioB2B.Infrastructure.Http.Handlers;
using StudioB2B.Infrastructure.Integrations.Ozon;
using StudioB2B.Infrastructure.MultiTenancy;
using StudioB2B.Infrastructure.MultiTenancy.CircuitHandlers;
using StudioB2B.Infrastructure.MultiTenancy.Initialization;
using StudioB2B.Infrastructure.MultiTenancy.Resolution;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Infrastructure.Services;
using System.Text;
using TenantService = StudioB2B.Infrastructure.MultiTenancy.Services.TenantService;
using StudioB2B.Infrastructure.MultiTenancy.Services;

namespace StudioB2B.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MultiTenancyOptions>(
            configuration.GetSection(MultiTenancyOptions.SectionName));

        services.AddAutoMapper(typeof(DependencyInjection).Assembly);

        services.AddDbContext<MasterDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("MasterDb");
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        });

        services.AddHostedService<DatabaseMigrationService>();

        services.AddSingleton<IKeyEncryptionService, KeyEncryptionService>();

        // ── HTTP pipeline for marketplace APIs (Ozon, etc.) ──
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
        services.AddScoped<IOrderSyncService, OrderSyncService>();
        services.AddScoped<IOzonChatService, OzonChatService>();

        services.AddScoped<TenantProvider>();
        services.AddScoped<ITenantProvider>(sp => sp.GetRequiredService<TenantProvider>());

        services.AddSingleton<ISubdomainResolver, SubdomainResolver>();
        services.AddScoped<ITenantDatabaseInitializer, TenantDatabaseInitializer>();
        services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();
        services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<CircuitHandler, TenantCircuitHandler>();

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
        services.AddScoped<Features.Orders.ITenantDbContextCreator, TenantDbContextCreator>();

        // ── JWT Authentication ─────────────────────────────────────────────────
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
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey  = true,
                    ValidIssuer              = issuer,
                    ValidAudience            = audience,
                    IssuerSigningKey         = key,
                    ClockSkew                = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        // ── Role Feature Classes ────────────────────────────────────────────────
        services.AddScoped<GetRoles>();
        services.AddScoped<GetRoleById>();
        services.AddScoped<CreateRole>();
        services.AddScoped<UpdateRole>();
        services.AddScoped<DeleteRole>();

        // ── User Feature Classes ───────────────────────────────────────────────
        services.AddScoped<GetUsers>();
        services.AddScoped<GetUserById>();
        services.AddScoped<GetAvailableRoles>();
        services.AddScoped<CreateUser>();
        services.AddScoped<UpdateUser>();
        services.AddScoped<DeleteUser>();

        // ── Hangfire per-tenant manager ────────────────────────────────────────
        services.AddSingleton<TenantHangfireManager>();
        services.AddHostedService(sp => sp.GetRequiredService<TenantHangfireManager>());

        // ── Background job services ────────────────────────────────────────────
        services.AddScoped<IOrderSyncJobService, OrderSyncJobService>();

        // ── Transaction services ───────────────────────────────────────────────
        services.AddScoped<CalculationEngine>();
        services.AddScoped<IOrderTransactionService, OrderTransactionService>();

        return services;
    }
}
