namespace StudioB2B.Domain.Options;

public class BackupOptions
{
    public const string SectionName = "Backup";

    /// <summary>MinIO endpoint, например "minio:9000".</summary>
    public string Endpoint { get; set; } = "minio:9000";

    /// <summary>Публичный endpoint для presigned URLs (если MinIO доступен снаружи). Если пусто — используется Endpoint.</summary>
    public string? PublicEndpoint { get; set; }

    public string Bucket { get; set; } = "backups";

    public string AccessKey { get; set; } = "";

    public string SecretKey { get; set; } = "";

    public bool UseSSL { get; set; } = false;

    /// <summary>Количество дней хранения по умолчанию.</summary>
    public int DefaultRetentionDays { get; set; } = 30;

    /// <summary>Путь к бинарнику mysqldump. По умолчанию "mysqldump" (работает в Docker/Linux).</summary>
    public string MysqldumpPath { get; set; } = "mysqldump";
}
