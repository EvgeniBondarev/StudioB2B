using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Domain.Options;
using StudioB2B.Infrastructure.MultiTenancy;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Infrastructure.Services;
using TenantService = StudioB2B.Infrastructure.MultiTenancy.TenantService;

namespace StudioB2B.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MultiTenancyOptions>(
            configuration.GetSection(MultiTenancyOptions.SectionName));

        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        services.AddAutoMapper(typeof(DependencyInjection).Assembly);

        services.AddDbContext<MasterDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("MasterDb");
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        });

        services.AddHostedService<DatabaseMigrationService>();

        services.AddScoped<TenantProvider>();
        services.AddScoped<ITenantProvider>(sp => sp.GetRequiredService<TenantProvider>());

        services.AddSingleton<ISubdomainResolver, SubdomainResolver>();
        services.AddScoped<ITenantDatabaseInitializer, TenantDatabaseInitializer>();
        services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<CircuitHandler, TenantCircuitHandler>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped(sp =>
        {
            var tenantProvider = sp.GetRequiredService<ITenantProvider>();

            if (!tenantProvider.IsResolved)
            {
                throw new InvalidOperationException("Tenant is not resolved. Ensure TenantMiddleware is configured.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            optionsBuilder.UseMySql(
                tenantProvider.ConnectionString!,
                ServerVersion.AutoDetect(tenantProvider.ConnectionString!));

            return new TenantDbContext(optionsBuilder.Options);
        });

        // JWT Authentication — token is read from HttpOnly cookie "auth_token"
        var jwtSecret = configuration[$"{JwtOptions.SectionName}:{nameof(JwtOptions.Secret)}"]
                        ?? throw new InvalidOperationException("JWT Secret is not configured.");
        var jwtIssuer = configuration[$"{JwtOptions.SectionName}:{nameof(JwtOptions.Issuer)}"] ?? "StudioB2B";
        var jwtAudience = configuration[$"{JwtOptions.SectionName}:{nameof(JwtOptions.Audience)}"] ?? "StudioB2B";

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
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
                };

                // Read JWT from HttpOnly cookie instead of Authorization header
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        ctx.Token = ctx.Request.Cookies["auth_token"];
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }
}
