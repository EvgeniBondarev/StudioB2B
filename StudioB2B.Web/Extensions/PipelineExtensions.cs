using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Serilog;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Infrastructure.Services.MultiTenancy;
using StudioB2B.Web.Components;
using StudioB2B.Web.Hubs;

namespace StudioB2B.Web.Extensions;

public static class PipelineExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        // Перехват ошибок только для не-API маршрутов.
        // API эндпоинты возвращают JSON/plain-text статусы напрямую;
        // re-execute для них приводит к ошибке "Incorrect Content-Type" в Blazor.
        app.UseWhen(
            ctx => !ctx.Request.Path.StartsWithSegments("/api"),
            a => a.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));

        app.UseHttpsRedirection();

        app.UseForwardedHeaders();
        app.UseCors("AllowSubdomains");
        app.UseMiddleware<TenantMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.Use(async (ctx, next) =>
        {
            await next();

            if (!ctx.Request.Path.StartsWithSegments("/api"))
                return;

            if (ctx.Response.StatusCode is not (StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden))
                return;

            // #region agent log
            _ = System.IO.File.AppendAllTextAsync(
                "/Users/evgen/RiderProjects/StudioB2B/.cursor/debug-3f5ec5.log",
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    sessionId = "3f5ec5",
                    runId = "pre-fix-2",
                    hypothesisId = "H7",
                    location = "PipelineExtensions.cs:UnauthorizedApiMiddleware",
                    message = "API returned 401/403",
                    data = new
                    {
                        path = ctx.Request.Path.Value,
                        method = ctx.Request.Method,
                        status = ctx.Response.StatusCode,
                        endpoint = ctx.GetEndpoint()?.DisplayName,
                        hasAuthHeader = ctx.Request.Headers.ContainsKey("Authorization"),
                        isAuthenticated = ctx.User.Identity?.IsAuthenticated ?? false
                    },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }) + Environment.NewLine,
                CancellationToken.None);
            // #endregion
        });
        // Обязателен для Blazor Interactive Server.
        // IAntiforgery заменён на NoOpAntiforgery — реальная валидация отключена.
        app.UseAntiforgery();

        app.MapControllers();
        app.UseStaticFiles();
        app.MapStaticAssets();

        app.MapHub<SyncNotificationHub>("/hubs/sync");
        app.MapHub<TaskBoardHub>("/hubs/taskboard");
        app.MapHub<OzonPushHub>("/hubs/ozon-push");

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AllowAnonymous();

        // Применяем pending миграции для всех существующих тенантов при старте
        _ = Task.Run(() => MigrateAllTenantsAsync(app));

        return app;
    }

    private static async Task MigrateAllTenantsAsync(WebApplication app)
    {
        try
        {
            await using var scope = app.Services.CreateAsyncScope();
            var masterDb = scope.ServiceProvider.GetRequiredService<MasterDbContext>();
            var initializer = scope.ServiceProvider.GetRequiredService<ITenantDatabaseInitializer>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<WebApplication>>();

            var tenants = await masterDb.Tenants
                .Select(t => new { t.Id, t.ConnectionString, t.Subdomain })
                .ToListAsync();

            logger.LogInformation("Startup: migrating {Count} tenant database(s).", tenants.Count);

            foreach (var tenant in tenants)
            {
                try
                {
                    await initializer.MigrateOnlyAsync(tenant.ConnectionString, tenant.Subdomain, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Startup: failed to migrate tenant {TenantId}.", tenant.Id);
                }
            }

            logger.LogInformation("Startup: all tenant migrations completed.");
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetRequiredService<ILogger<WebApplication>>();
            logger.LogError(ex, "Startup: tenant migration runner failed.");
        }
    }
}
