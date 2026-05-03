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
using Microsoft.Extensions.Options;
using Minio;
using StudioB2B.Domain.Options;
using StudioB2B.Infrastructure.Helpers.Http.Handlers;
using StudioB2B.Infrastructure.Authorization;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Services.MultiTenancy;
using StudioB2B.Infrastructure.Services.Modules;
using StudioB2B.Infrastructure.Services.Order;
using StudioB2B.Infrastructure.Services.Communication;
using StudioB2B.Infrastructure.Services.Ozon;
using TenantService = StudioB2B.Infrastructure.Services.MultiTenancy.TenantService;

namespace StudioB2B.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MultiTenancyOptions>(
            configuration.GetSection(MultiTenancyOptions.SectionName));

        services.Configure<OzonOptions>(
            configuration.GetSection(OzonOptions.SectionName));

        services.Configure<OpenRouterOptions>(
            configuration.GetSection(OpenRouterOptions.SectionName));

        services.Configure<EncryptionOptions>(
            configuration.GetSection(EncryptionOptions.SectionName));

        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        services.Configure<SeedOptions>(
            configuration.GetSection(SeedOptions.SectionName));

        services.Configure<ManufacturersModuleOptions>(
            configuration.GetSection(ManufacturersModuleOptions.SectionName));

        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(DependencyInjection).Assembly));

        services.AddDbContext<MasterDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("MasterDb");
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        });

        services.AddHostedService<DatabaseMigrationService>();

        services.Configure<BackupOptions>(
            configuration.GetSection(BackupOptions.SectionName));

        services.Configure<EmailOptions>(
            configuration.GetSection(EmailOptions.SectionName));

        services.AddSingleton<BackgroundEmailSenderService>();
        services.AddSingleton<IEmailService>(sp => sp.GetRequiredService<BackgroundEmailSenderService>());
        services.AddHostedService(sp => sp.GetRequiredService<BackgroundEmailSenderService>());

        services.AddMemoryCache();

        services.AddSingleton<IKeyEncryptionService, KeyEncryptionService>();

        services.AddTransient<LoggingHandler>();
        services.AddTransient<RetryHandler>();
        services.AddTransient<RateLimitHandler>();

        var ozonOpts = configuration.GetSection(OzonOptions.SectionName).Get<OzonOptions>() ?? new OzonOptions();

        services.AddHttpClient("Ozon", client =>
        {
            client.BaseAddress = new Uri(ozonOpts.BaseAddress);
            client.Timeout = TimeSpan.FromSeconds(ozonOpts.TimeoutSeconds);
        })
        .AddHttpMessageHandler<LoggingHandler>()
        .AddHttpMessageHandler<RetryHandler>()
        .AddHttpMessageHandler<RateLimitHandler>();

        var openRouterOpts = configuration.GetSection(OpenRouterOptions.SectionName).Get<OpenRouterOptions>() ?? new OpenRouterOptions();
        services.AddHttpClient("OpenRouter", client =>
        {
            client.BaseAddress = new Uri(openRouterOpts.BaseAddress);
            client.Timeout = TimeSpan.FromSeconds(openRouterOpts.TimeoutSeconds);
        });

        services.AddScoped<IOzonApiClient, OzonApiClient>();
        services.AddScoped<IOrderAdapter, OzonFbsOrderAdapter>();
        services.AddScoped<IOrderAdapter, OzonFboOrderAdapter>();
        services.AddScoped<IOrderSyncService, OrderSyncService>();
        services.AddScoped<IOzonChatService, OzonChatService>();
        services.AddScoped<IOzonQuestionsService, OzonQuestionsService>();
        services.AddScoped<IOzonReviewsService, OzonReviewsService>();
        services.AddScoped<IOzonPushNotificationService, OzonPushNotificationService>();
        services.AddScoped<IOpenRouterService, OpenRouterService>();

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
        services.AddScoped<IMasterUserService, MasterUserService>();

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

        var jwtOpts = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var secret = !string.IsNullOrEmpty(jwtOpts.Secret)
            ? jwtOpts.Secret
            : throw new InvalidOperationException("Jwt:Secret is not configured");
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
                    ValidIssuer = jwtOpts.Issuer,
                    ValidAudience = jwtOpts.Audience,
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.Zero
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        // #region agent log
                        _ = System.IO.File.AppendAllTextAsync(
                            "/Users/evgen/RiderProjects/StudioB2B/.cursor/debug-3f5ec5.log",
                            System.Text.Json.JsonSerializer.Serialize(new
                            {
                                sessionId = "3f5ec5",
                                runId = "pre-fix-2",
                                hypothesisId = "H5",
                                location = "DependencyInjection.cs:JwtBearerEvents.OnMessageReceived",
                                message = "JWT message received",
                                data = new
                                {
                                    path = ctx.Request.Path.Value,
                                    hasAuthorizationHeader = ctx.Request.Headers.ContainsKey("Authorization"),
                                    headerPrefix = ctx.Request.Headers.Authorization.ToString().Split(' ').FirstOrDefault()
                                },
                                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                            }) + Environment.NewLine,
                            CancellationToken.None);
                        // #endregion
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = ctx =>
                    {
                        // #region agent log
                        _ = System.IO.File.AppendAllTextAsync(
                            "/Users/evgen/RiderProjects/StudioB2B/.cursor/debug-3f5ec5.log",
                            System.Text.Json.JsonSerializer.Serialize(new
                            {
                                sessionId = "3f5ec5",
                                runId = "pre-fix-2",
                                hypothesisId = "H6",
                                location = "DependencyInjection.cs:JwtBearerEvents.OnTokenValidated",
                                message = "JWT token validated",
                                data = new
                                {
                                    path = ctx.Request.Path.Value,
                                    userId = ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                        ?? ctx.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                                },
                                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                            }) + Environment.NewLine,
                            CancellationToken.None);
                        // #endregion
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = ctx =>
                    {
                        // #region agent log
                        _ = System.IO.File.AppendAllTextAsync(
                            "/Users/evgen/RiderProjects/StudioB2B/.cursor/debug-3f5ec5.log",
                            System.Text.Json.JsonSerializer.Serialize(new
                            {
                                sessionId = "3f5ec5",
                                runId = "pre-fix-2",
                                hypothesisId = "H6",
                                location = "DependencyInjection.cs:JwtBearerEvents.OnAuthenticationFailed",
                                message = "JWT authentication failed",
                                data = new
                                {
                                    path = ctx.Request.Path.Value,
                                    error = ctx.Exception.Message
                                },
                                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                            }) + Environment.NewLine,
                            CancellationToken.None);
                        // #endregion
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        services.AddSingleton<IAuthorizationHandler, AdminSatisfiesAllRolesHandler>();

        // Feature operations moved to TenantDbContext extension methods (e.g. db.GetUsersAsync(...)).
        // No scoped registrations required for these extension methods.

        // Business logic services (wrapping Features for UI layer)
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<ICalculationRuleService, CalculationRuleService>();
        services.AddScoped<IMarketplaceClientService, MarketplaceClientService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IReturnsService, ReturnsService>();
        services.AddScoped<IOrderStatusService, OrderStatusService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IPriceTypeService, PriceTypeService>();

        services.AddScoped<IEntityFilterService, EntityFilterService>();

        services.AddSingleton<TenantHangfireManager>();
        services.AddHostedService(sp => sp.GetRequiredService<TenantHangfireManager>());

        services.AddSingleton<MasterHangfireManager>();
        services.AddHostedService(sp => sp.GetRequiredService<MasterHangfireManager>());

        services.AddSingleton<IMinioClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<BackupOptions>>().Value;
            return new MinioClient()
                .WithEndpoint(opts.Endpoint)
                .WithCredentials(opts.AccessKey, opts.SecretKey)
                .WithSSL(opts.UseSSL)
                .Build();
        });

        services.AddScoped<IOrderSyncJobService, OrderSyncJobService>();

        services.AddScoped<CalculationEngine>();
        services.AddScoped<IOrderTransactionService, OrderTransactionService>();
        services.AddScoped<IOrderTransactionManagementService, OrderTransactionManagementService>();

        services.AddScoped<IModuleService, ModuleService>();
        services.AddScoped<IModuleActivator, ManufacturerModuleActivator>();

        services.AddScoped<ICommunicationTaskService, CommunicationTaskService>();
        services.AddScoped<ICommunicationTaskSyncService, CommunicationTaskSyncService>();
        services.AddTransient<CommunicationSyncHangfireJob>();

        services.AddScoped<ITenantBackupService, TenantBackupService>();

        return services;
    }
}
