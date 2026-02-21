using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using MudBlazor.Services;
using Serilog;
using StudioB2B.Infrastructure;
using StudioB2B.Infrastructure.MultiTenancy;
using StudioB2B.Web.Components;
using Microsoft.AspNetCore.HttpOverrides;

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

    builder.Services.AddInfrastructure(builder.Configuration);

    // Add MudBlazor services
    builder.Services.AddMudServices();

    // 🔥 Настройка аутентификации для субдоменов
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.Cookie.Domain = ".studiob2b.ru"; // Точка в начале для всех субдоменов
        options.Cookie.Name = ".AspNetCore.Identity.Application";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;

        // Важно для Blazor
        options.Cookie.IsEssential = true;
    });

    // 🔥 Настройка CORS для субдоменов
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowSubdomains", policy =>
        {
            policy.SetIsOriginAllowed(origin =>
            {
                var uri = new Uri(origin);
                return uri.Host == "studiob2b.ru" ||
                       uri.Host.EndsWith(".studiob2b.ru");
            })
            .AllowCredentials()  // Критически важно для куки
            .AllowAnyHeader()
            .AllowAnyMethod();
        });
    });

    // 🔥 Добавьте поддержку форварднутых заголовков от Nginx
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                   ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    builder.Services.AddControllers();

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Произошла внутренняя ошибка сервера.");
            });
        });
        app.UseHsts();
    }

    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();

    // 🔥 Добавить для работы с прокси (должно быть до других middleware)
    app.UseForwardedHeaders();

    // 🔥 Добавить CORS
    app.UseCors("AllowSubdomains");

    // Tenant resolution (must be before Authentication)
    app.UseTenantResolution();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseAntiforgery();

    app.MapControllers();
    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    // Health check endpoint for Docker/Kubernetes
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

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
