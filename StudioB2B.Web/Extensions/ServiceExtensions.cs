using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.HttpOverrides;
using Radzen;
using StudioB2B.Infrastructure;
using Microsoft.AspNetCore.Components.Authorization;
using StudioB2B.Web.Services;

namespace StudioB2B.Web.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceProvider = services.BuildServiceProvider();
        var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();

        StaticWebAssetsLoader.UseStaticWebAssets(environment, configuration);

        services.AddHttpContextAccessor();

        services.AddInfrastructure(configuration);
        services.AddScoped<AuthenticationStateProvider, CookieAuthenticationStateProvider>();

        services.AddScoped<DialogService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<TooltipService>();
        services.AddScoped<ContextMenuService>();

        ConfigureCors(services, environment);

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
            if (environment.IsDevelopment())
                options.AllowedHosts.Clear();
        });

        services.AddControllers();
        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        return services;
    }

    private static void ConfigureCors(IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowSubdomains", policy =>
            {
                var host = environment.IsDevelopment() ? "localhost": "studiob2b.ru";
                policy.SetIsOriginAllowed(origin =>
                    {
                        var uri = new Uri(origin);
                        return uri.Host == host || uri.Host.EndsWith($".{host}");
                    })
                    .AllowCredentials()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
    }
}
