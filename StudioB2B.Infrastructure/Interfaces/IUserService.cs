using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Сервис для работы с пользователями тенанта
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Получить список всех пользователей
    /// </summary>
    Task<List<UserListDto>> GetAllUsersAsync(CancellationToken ct = default);

    /// <summary>
    /// Получить пользователя по ID
    /// </summary>
    Task<UserListDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Создать нового пользователя (неактивного).
    /// После создания на email отправляется ссылка активации.
    /// </summary>
    Task<(bool Success, string? Error)> CreateUserAsync(CreateUserDto request, string tenantBaseUrl, CancellationToken ct = default);

    /// <summary>
    /// Обновить пользователя
    /// </summary>
    Task<(bool Success, string? Error)> UpdateUserAsync(Guid id, UpdateUserDto request, CancellationToken ct = default);

    /// <summary>
    /// Удалить пользователя (мягкое удаление)
    /// </summary>
    Task<(bool Success, string? Error)> DeleteUserAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Сменить пароль пользователя
    /// </summary>
    Task<(bool Success, string? Error)> ChangePasswordAsync(ChangeUserPasswordDto dto, CancellationToken ct = default);

    /// <summary>
    /// Активировать пользователя по токену из письма.
    /// </summary>
    Task<(bool Success, string? Error)> ActivateUserAsync(Guid tokenId, CancellationToken ct = default);
}
