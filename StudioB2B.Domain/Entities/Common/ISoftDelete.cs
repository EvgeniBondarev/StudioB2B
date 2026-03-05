namespace StudioB2B.Domain.Entities.Common;

public interface ISoftDelete
{
    [SkipAudit]
    bool IsDeleted { get; set; }
}
