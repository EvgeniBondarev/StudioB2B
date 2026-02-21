using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using MudBlazor.Services;
using Serilog;
using StudioB2B.Infrastructure;
using StudioB2B.Infrastructure.MultiTenancy;
using StudioB2B.Web.Components;

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
