namespace StudioB2B.Application.Common.Interfaces;

/// <summary>
/// Интерфейс для получения информации о текущем пользователе
/// </summary>
public interface ICurrentUserProvider
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    IEnumerable<string> Permissions { get; }
}
