using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.HttpOverrides;
using MudBlazor.Services;
using StudioB2B.Infrastructure;
using Microsoft.AspNetCore.Components;

namespace StudioB2B.Web.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Получаем environment правильным способом
        var serviceProvider = services.BuildServiceProvider();
        var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();

        StaticWebAssetsLoader.UseStaticWebAssets(environment, configuration);

        services.AddHttpContextAccessor();
        // provide HttpClient for components (Blazor Server doesn't add it by default)
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
        services.AddMudServices();

        // Настройка аутентификации в зависимости от окружения
        ConfigureAuthentication(services, environment);

        // Настройка CORS в зависимости от окружения
        ConfigureCors(services, environment);

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                       ForwardedHeaders.XForwardedProto;

            if (environment.IsDevelopment())
            {
                // В разработке добавляем локальные адреса
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
                options.AllowedHosts.Clear(); // Разрешаем все хосты в разработке
            }
            else
            {
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            }
        });

        services.AddControllers();
        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        return services;
    }

    private static void ConfigureAuthentication(IServiceCollection services, IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            // Для локальной разработки с localhost
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Domain = null; // Не устанавливать домен для localhost
                options.Cookie.Name = ".AspNetCore.Identity.Application";
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Важно для localhost (не HTTPS)
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
            });
        }
        else
        {
            // Для Production
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Domain = ".studiob2b.ru";
                options.Cookie.Name = ".AspNetCore.Identity.Application";
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
            });
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
                    // В разработке разрешаем все localhost с любыми портами
                    policy.SetIsOriginAllowed(origin =>
                    {
                        var uri = new Uri(origin);
                        // Разрешаем localhost с любым портом и любой схемой (http/https)
                        return uri.Host == "localhost" ||
                               uri.Host.EndsWith(".localhost") ||
                               uri.Host == "127.0.0.1";
                    })
                    .AllowCredentials()  // Критически важно для куки
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                }
                else
                {
                    // В продакшене - строгая проверка
                    policy.SetIsOriginAllowed(origin =>
                    {
                        var uri = new Uri(origin);
                        return uri.Host == "studiob2b.ru" ||
                               uri.Host.EndsWith(".studiob2b.ru");
                    })
                    .AllowCredentials()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                }
            });
        });
    }
}
