using Microsoft.AspNetCore.Antiforgery;

namespace StudioB2B.Web.Infrastructure;

/// <summary>
/// No-op реализация IAntiforgery — полностью отключает CSRF-валидацию.
/// Используется в связке с JWT-аутентификацией, где CSRF не актуален.
/// </summary>
internal sealed class NoOpAntiforgery : IAntiforgery
{
    public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
        => new(null, null, "__RequestVerificationToken", "form");

    public AntiforgeryTokenSet GetTokens(HttpContext httpContext)
        => new(null, null, "__RequestVerificationToken", "form");

    public Task<bool> IsRequestValidAsync(HttpContext httpContext)
        => Task.FromResult(true);

    public void SetCookieTokenAndHeader(HttpContext httpContext) { }

    public Task ValidateRequestAsync(HttpContext httpContext)
        => Task.CompletedTask;
}

