namespace StudioB2B.Shared.DTOs;

public class ReturnsSyncResultDto
{
    public int Created { get; set; }
    public int Updated { get; set; }
    /// <summary>Отправлений, которым проставлен HasReturn = true.</summary>
    public int Linked  { get; set; }
}
