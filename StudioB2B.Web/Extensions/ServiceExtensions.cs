using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using StudioB2B.Web.Infrastructure;
using StudioB2B.Web.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Radzen;
using StudioB2B.Infrastructure;
using Microsoft.AspNetCore.Components;
using StudioB2B.Infrastructure.Interfaces;

namespace StudioB2B.Web.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var serviceProvider = services.BuildServiceProvider();
        var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();

        StaticWebAssetsLoader.UseStaticWebAssets(environment, configuration);

        services.AddHttpContextAccessor();
        services.AddHttpClient();
        services.AddScoped(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var nav = sp.GetRequiredService<NavigationManager>();
            var client = factory.CreateClient();
            client.BaseAddress = new Uri(nav.BaseUri);
            return client;
        });

        services.AddInfrastructure(configuration);

        services.AddSignalR();
        services.AddScoped<ISyncNotificationSender, SyncNotificationSender>();

        services.AddScoped<DialogService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<TooltipService>();
        services.AddScoped<ContextMenuService>();
        services.AddScoped<TabService>();

        ConfigureCors(services, environment);

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            if (environment.IsDevelopment())
                options.AllowedHosts.Clear();
        });

        services.AddControllers();
        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // JWT AuthenticationStateProvider для Blazor
        services.AddScoped<JwtAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<JwtAuthenticationStateProvider>());

        // JWT используется для аутентификации — CSRF-защита не нужна.
        services.AddSingleton<IAntiforgery, NoOpAntiforgery>();

        return services;
    }

    private static void ConfigureCors(IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowSubdomains", policy =>
            {
                if (environment.IsDevelopment())
                {
                    policy.SetIsOriginAllowed(origin =>
                    {
                        var uri = new Uri(origin);
                        return uri.Host == "localhost" || uri.Host.EndsWith(".localhost") || uri.Host == "127.0.0.1";
                    })
                    .AllowCredentials()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                }
                else
                {
                    policy.SetIsOriginAllowed(origin =>
                    {
                        var uri = new Uri(origin);
                        return uri.Host == "studiob2b.ru" || uri.Host.EndsWith(".studiob2b.ru");
                    })
                    .AllowCredentials()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                }
            });
        });
    }
}
