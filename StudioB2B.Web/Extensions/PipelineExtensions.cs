using Serilog;
using StudioB2B.Infrastructure.MultiTenancy;
using StudioB2B.Web.Components;

namespace StudioB2B.Web.Extensions;

public static class PipelineExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    await context.Response.WriteAsync("Произошла внутренняя ошибка сервера.");
                });
            });
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();

        // Важно: порядок middleware
        app.UseForwardedHeaders();
        app.UseCors("AllowSubdomains");
        app.UseTenantResolution(); // Должно быть до аутентификации
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();

        app.MapControllers();
        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        // Health check
        app.MapGet("/health", () =>
                       Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

        return app;
    }
}
