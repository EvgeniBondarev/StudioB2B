using System.Threading.Channels;

namespace StudioB2B.Infrastructure.BackgroundJobs;

/// <summary>
/// Тип операции синхронизации роли
/// </summary>
public enum RoleSyncOperation { Upsert, Delete }

/// <summary>
/// Задание на синхронизацию роли во все тенанты
/// </summary>
public record RoleSyncJob(
    Guid RoleId,
    string RoleName,
    string NormalizedRoleName,
    string? Description,
    bool IsSystemRole,
    RoleSyncOperation Operation);

/// <summary>
/// Канал для передачи заданий синхронизации ролей в фоновый воркер
/// </summary>
public class RoleSyncChannel
{
    private readonly Channel<RoleSyncJob> _channel =
        Channel.CreateUnbounded<RoleSyncJob>(new UnboundedChannelOptions
        {
            SingleReader = true,
            AllowSynchronousContinuations = false
        });

    public ChannelWriter<RoleSyncJob> Writer => _channel.Writer;
    public ChannelReader<RoleSyncJob> Reader => _channel.Reader;

    public void Publish(RoleSyncJob job) => _channel.Writer.TryWrite(job);
}

