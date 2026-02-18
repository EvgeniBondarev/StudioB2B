namespace StudioB2B.Infrastructure.Services;

/// <summary>
/// Настройки базы данных
/// </summary>
public class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Применять миграции автоматически при запуске
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; set; }
}
