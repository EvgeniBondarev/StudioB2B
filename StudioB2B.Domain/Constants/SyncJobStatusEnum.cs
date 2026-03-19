namespace StudioB2B.Domain.Constants;

public enum SyncJobStatusEnum
{
    Enqueued = 1,
    Processing = 2,
    Succeeded = 3,
    Failed = 4,
    Cancelled = 5
}
