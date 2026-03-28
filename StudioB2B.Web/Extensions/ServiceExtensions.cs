using System.Net.Http;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Http;
using StudioB2B.Web.Infrastructure;
using StudioB2B.Web.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Radzen;
using StudioB2B.Infrastructure;
using StudioB2B.Infrastructure.Interfaces;

namespace StudioB2B.Web.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        StaticWebAssetsLoader.UseStaticWebAssets(environment, configuration);

        services.AddHttpContextAccessor();
        services.AddScoped<AuthTokenHandler>();
        services.AddHttpClient("Anonymous", (sp, client) =>
            ApplyBaseAddress(sp, client, configuration));
        services.AddHttpClient("StudioB2B", (sp, client) =>
            ApplyBaseAddress(sp, client, configuration)).AddHttpMessageHandler<AuthTokenHandler>();
        services.AddScoped<JwtAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<JwtAuthenticationStateProvider>());
        services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("StudioB2B"));

        services.AddInfrastructure(configuration);

        services.AddSignalR();
        services.AddScoped<ISyncNotificationSender, SyncNotificationSender>();
        services.AddScoped<ITaskBoardNotificationSender, TaskBoardNotificationSender>();

        services.AddScoped<DialogService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<TooltipService>();
        services.AddScoped<ContextMenuService>();
        services.AddScoped<TabService>();
        services.AddSingleton<NavService>();
        services.AddSingleton<PageRegistry>();

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

        services.AddScoped<MasterAuthStateService>();
        services.AddScoped<IMasterAuthApiService, MasterAuthApiService>();

        // JWT используется для аутентификации — CSRF-защита не нужна.
        services.AddSingleton<IAntiforgery, NoOpAntiforgery>();

        return services;
    }

    private static void ApplyBaseAddress(IServiceProvider sp, HttpClient client, IConfiguration configuration)
    {
        var httpContext = sp.GetService<IHttpContextAccessor>()?.HttpContext;
        if (httpContext?.Request != null)
        {
            var req = httpContext.Request;
            client.BaseAddress = new Uri($"{req.Scheme}://{req.Host}{req.PathBase}/");
        }
        else
        {
            var baseUrl = configuration["App:BaseUrl"]?.TrimEnd('/');
            client.BaseAddress = !string.IsNullOrEmpty(baseUrl)
                ? new Uri(baseUrl + "/")
                : new Uri("http://localhost:5184/");
        }
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
