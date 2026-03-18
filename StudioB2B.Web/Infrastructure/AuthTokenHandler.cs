using System.Net.Http.Headers;
using StudioB2B.Web.Services;

namespace StudioB2B.Web.Infrastructure;

/// <summary>
/// Добавляет JWT из localStorage к каждому исходящему HTTP-запросу.
/// Без этого API получает анонимные запросы и возвращает 401.
/// </summary>
public class AuthTokenHandler : DelegatingHandler
{
    private readonly JwtAuthenticationStateProvider _authProvider;

    public AuthTokenHandler(JwtAuthenticationStateProvider authProvider)
    {
        _authProvider = authProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _authProvider.GetTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
