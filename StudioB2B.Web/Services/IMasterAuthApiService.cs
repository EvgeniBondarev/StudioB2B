namespace StudioB2B.Web.Services;

/// <summary>
/// Клиентский сервис master-авторизации: инкапсулирует HTTP-вызов к API,
/// сохранение токена и обновление состояния аутентификации.
/// </summary>
public interface IMasterAuthApiService
{
    /// <summary>
    /// Войти в master-аккаунт по email и паролю.
    /// Возвращает сообщение об ошибке, или <c>null</c> при успехе.
    /// </summary>
    Task<string?> LoginAsync(string email, string password);

    /// <summary>
    /// Шаг 1 регистрации: создаёт пользователя и отправляет код на email.
    /// Возвращает сообщение об ошибке, или <c>null</c> при успехе.
    /// </summary>
    Task<string?> RegisterAsync(
        string  email,
        string  password,
        string  firstName,
        string  lastName,
        string? middleName);

    /// <summary>
    /// Шаг 2 регистрации: подтверждает код и выполняет вход.
    /// Возвращает сообщение об ошибке, или <c>null</c> при успехе.
    /// </summary>
    Task<string?> VerifyEmailAsync(string email, string code);

    /// <summary>
    /// Повторная отправка кода подтверждения.
    /// Возвращает сообщение об ошибке, или <c>null</c> при успехе.
    /// </summary>
    Task<string?> ResendCodeAsync(string email);
}
