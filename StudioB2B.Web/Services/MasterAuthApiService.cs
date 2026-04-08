using StudioB2B.Shared;

namespace StudioB2B.Web.Services;

/// <summary>
/// Реализация <see cref="IMasterAuthApiService"/>:
/// вызывает REST API master-авторизации, сохраняет JWT-токен
/// и обновляет состояние аутентификации в Blazor-цепи.
/// </summary>
public class MasterAuthApiService : IMasterAuthApiService
{
    private readonly HttpClient                    _http;
    private readonly JwtAuthenticationStateProvider _authProvider;
    private readonly MasterAuthStateService        _masterAuthState;

    public MasterAuthApiService(
        HttpClient http,
        JwtAuthenticationStateProvider authProvider,
        MasterAuthStateService masterAuthState)
    {
        _http = http;
        _authProvider = authProvider;
        _masterAuthState = masterAuthState;
    }

    /// <inheritdoc/>
    public async Task<string?> LoginAsync(string email, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/master/auth/login",
                new { Email = email, Password = password });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
                if (result?.Token is not null)
                {
                    await _authProvider.MasterLoginAsync(result.Token);
                    var userInfo = await _authProvider.GetMasterUserInfoAsync();
                    _masterAuthState.Set(true, userInfo);
                    return null; // успех
                }
            }

            var err = await TryReadErrorAsync(response);
            return err ?? "Неверный email или пароль";
        }
        catch
        {
            return "Произошла ошибка при входе";
        }
    }

    /// <inheritdoc/>
    public async Task<string?> RegisterAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        string? middleName)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/master/auth/register", new
            {
                Email = email,
                Password = password,
                FirstName = firstName,
                LastName = lastName,
                MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName
            });

            if (response.IsSuccessStatusCode)
                return null; // успех

            var err = await TryReadErrorAsync(response);
            return err ?? "Ошибка регистрации";
        }
        catch
        {
            return "Произошла ошибка при регистрации";
        }
    }

    /// <inheritdoc/>
    public async Task<string?> VerifyEmailAsync(string email, string code)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/master/auth/verify-email",
                new { Email = email, Code = code });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
                if (result?.Token is not null)
                {
                    await _authProvider.MasterLoginAsync(result.Token);
                    var userInfo = await _authProvider.GetMasterUserInfoAsync();
                    _masterAuthState.Set(true, userInfo);
                    return null; // успех
                }
            }

            var err = await TryReadErrorAsync(response);
            return err ?? "Неверный код подтверждения";
        }
        catch
        {
            return "Произошла ошибка при подтверждении";
        }
    }

    /// <inheritdoc/>
    public async Task<string?> ResendCodeAsync(string email)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/master/auth/resend-code",
                new { Email = email });

            if (response.IsSuccessStatusCode)
                return null; // успех

            var err = await TryReadErrorAsync(response);
            return err ?? "Не удалось отправить код";
        }
        catch
        {
            return "Произошла ошибка при отправке кода";
        }
    }

    private static async Task<string?> TryReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var err = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return err?.Error;
        }
        catch { return null; }
    }

    private sealed record ErrorResponse(string? Error);
}

