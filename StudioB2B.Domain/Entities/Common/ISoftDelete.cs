namespace StudioB2B.Domain.Entities;

public interface ISoftDelete
{
    [SkipAudit]
    bool IsDeleted { get; set; }
}
