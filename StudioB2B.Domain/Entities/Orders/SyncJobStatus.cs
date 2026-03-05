namespace StudioB2B.Domain.Entities.Orders;

public enum SyncJobStatus
{
    Enqueued   = 1,
    Processing = 2,
    Succeeded  = 3,
    Failed     = 4,
    Cancelled  = 5
}

