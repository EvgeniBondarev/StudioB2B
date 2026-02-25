using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.HttpOverrides;
using MudBlazor.Services;
using StudioB2B.Infrastructure;
using Microsoft.AspNetCore.Components;
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
        services.AddSingleton<TokenExchangeService>();
        services.AddScoped<CookieAuthService>();
        services.AddScoped<AuthenticationStateProvider, CookieAuthenticationStateProvider>();
        services.AddMudServices();

        ConfigureCors(services, environment);

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                       ForwardedHeaders.XForwardedProto;

            if (environment.IsDevelopment())
            {
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
                options.AllowedHosts.Clear();
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
                        return uri.Host == "localhost" ||
                               uri.Host.EndsWith(".localhost") ||
                               uri.Host == "127.0.0.1";
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
