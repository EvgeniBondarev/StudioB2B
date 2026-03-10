namespace StudioB2B.Infrastructure.Persistence.Tenant;

/// <summary>
/// Константы системного пользователя-робота.
/// Создаётся автоматически при инициализации каждого тенанта.
/// Используется как fallback при аудите изменений, когда HTTP-контекст недоступен.
/// </summary>
public static class SystemUser
{
    /// <summary>Фиксированный Id робота — одинаков во всех тенантах.</summary>
    public static readonly Guid RobotId = new("00000000-0000-0000-0000-000000000001");

    public const string RobotEmail     = "robot@system";
    public const string RobotFirstName = "Робот";
    public const string RobotLastName  = "Система";
}

