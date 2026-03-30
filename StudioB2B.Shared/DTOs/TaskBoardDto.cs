using StudioB2B.Domain.Constants;

namespace StudioB2B.Shared;

public class TaskBoardDto
{
    public List<CommunicationTaskDto> NewTasks { get; set; } = new();

    public List<CommunicationTaskDto> InProgressTasks { get; set; } = new();

    public List<CommunicationTaskDto> DoneTasks { get; set; } = new();

    /// <summary>Total Done/Cancelled count in the DB (for infinite scroll).</summary>
    public int DoneTotalCount { get; set; }

    public Dictionary<CommunicationTaskType, int> TypeCounts { get; set; } = new();

    /// <summary>Estimated flat PerTask payment per task type (global rates, no min duration).</summary>
    public Dictionary<CommunicationTaskType, decimal> PaymentEstimates { get; set; } = new();

    /// <summary>Hourly rate per task type (global rates, no min duration).</summary>
    public Dictionary<CommunicationTaskType, decimal> HourlyEstimates { get; set; } = new();
}

