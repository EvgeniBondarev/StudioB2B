namespace StudioB2B.Domain.Entities.Common;

public interface ISoftDelete
{
    bool IsDeleted { get; set; }
}
