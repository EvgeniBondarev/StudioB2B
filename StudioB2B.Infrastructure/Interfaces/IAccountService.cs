using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Interfaces;

public interface IAccountService
{
    /// <summary>
    /// Шаг 1 входа: проверяет учётные данные, генерирует код и отправляет его на email.
    /// Возвращает null, если email не найден, пользователь неактивен или пароль неверен.
    /// </summary>
    Task<TenantLoginInitResultDto?> InitiateLoginAsync(string email, string password, CancellationToken ct = default);

    /// <summary>
    /// Шаг 2 входа: проверяет код и возвращает данные для выпуска JWT.
    /// Возвращает null, если код неверный или устаревший.
    /// </summary>
    Task<AccountLoginResultDto?> VerifyLoginCodeAsync(string email, string code, CancellationToken ct = default);

    /// <summary>
    /// Повторная отправка кода входа.
    /// Возвращает false, если пользователь не найден.
    /// </summary>
    Task<bool> ResendLoginCodeAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Возвращает данные активного пользователя для перевыпуска JWT.
    /// Возвращает null, если пользователь не найден или неактивен.
    /// </summary>
    Task<AccountLoginResultDto?> RefreshAsync(Guid userId, CancellationToken ct = default);
}
