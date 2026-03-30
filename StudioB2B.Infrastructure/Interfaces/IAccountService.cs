using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Interfaces;

public interface IAccountService
{
    /// <summary>
    /// Проверяет учётные данные пользователя и возвращает данные для выпуска JWT.
    /// Возвращает null, если email не найден, пользователь неактивен или пароль неверен.
    /// </summary>
    Task<AccountLoginResultDto?> LoginAsync(string email, string password, CancellationToken ct = default);

    /// <summary>
    /// Возвращает данные активного пользователя для перевыпуска JWT.
    /// Возвращает null, если пользователь не найден или неактивен.
    /// </summary>
    Task<AccountLoginResultDto?> RefreshAsync(Guid userId, CancellationToken ct = default);
}

