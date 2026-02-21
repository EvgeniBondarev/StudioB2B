using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.HttpOverrides;
using MudBlazor.Services;
using Serilog;
using StudioB2B.Infrastructure;
using StudioB2B.Infrastructure.MultiTenancy;
using StudioB2B.Web.Components;
using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies; // Добавить
using Microsoft.AspNetCore.CookiePolicy; // Добавить

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting StudioB2B application...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, _, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

    builder.Services.AddHttpContextAccessor();

    // Настройка прокси
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                  ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
        options.KnownProxies.Add(IPAddress.Parse("127.0.0.1"));
    });

    // Добавляем аутентификацию и авторизацию
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.Cookie.Name = ".StudioB2B.Auth";
            options.Cookie.Domain = ".studiob2b.ru"; // Для всех субдоменов
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Для HTTPS
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.IsEssential = true;

            options.ExpireTimeSpan = TimeSpan.FromDays(14);
            options.SlidingExpiration = true;

            options.LoginPath = "/login";
            options.LogoutPath = "/logout";
            options.AccessDeniedPath = "/access-denied";

            // Важно для правильной работы через прокси
            options.Cookie.Path = "/";
        });

    builder.Services.AddAuthorization();

    builder.Services.AddInfrastructure(builder.Configuration);

    // Add MudBlazor services
    builder.Services.AddMudServices();

    builder.Services.AddControllers();

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    var app = builder.Build();

    // Важно: порядок middleware критичен!
    app.UseForwardedHeaders();

    app.UseSerilogRequestLogging();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    // Статические файлы должны быть до аутентификации
    app.MapStaticAssets();

    // Важно: CookiePolicy должен быть до аутентификации
    app.UseCookiePolicy(new CookiePolicyOptions
    {
        MinimumSameSitePolicy = SameSiteMode.Lax,
        Secure = CookieSecurePolicy.Always,
        HttpOnly = HttpOnlyPolicy.Always
    });

    // Tenant resolution ДО аутентификации
    app.UseTenantResolution();

    // Аутентификация и авторизация
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseAntiforgery();

    app.MapControllers();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    // Health check
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

    // Debug endpoint
    app.MapGet("/debug", async (HttpContext context) =>
    {
        var cookies = context.Request.Cookies.ToDictionary(c => c.Key, c => c.Value);
        var headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());

        return Results.Ok(new
        {
            host = context.Request.Host.Value,
            scheme = context.Request.Scheme,
            isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false,
            user = context.User?.Identity?.Name,
            cookies = cookies,
            headers = headers,
            tenant = context.Items["Tenant"] ?? "не определен"
        });
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
