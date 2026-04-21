namespace StudioB2B.Domain.Options;

public class BackupOptions
{
    public const string SectionName = "Backup";

    /// <summary>MinIO endpoint, например "minio:9000".</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Публичный endpoint для presigned URLs (если MinIO доступен снаружи). Если пусто — используется Endpoint.</summary>
    public string? PublicEndpoint { get; set; }

    public string Bucket { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public bool UseSSL { get; set; }

    /// <summary>Количество дней хранения по умолчанию.</summary>
    public int DefaultRetentionDays { get; set; }

    /// <summary>Путь к бинарнику mysqldump.</summary>
    public string MysqldumpPath { get; set; } = string.Empty;

    /// <summary>Путь к бинарнику mysql.</summary>
    public string MysqlPath { get; set; } = string.Empty;
}
