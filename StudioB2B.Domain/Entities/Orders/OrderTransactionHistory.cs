namespace StudioB2B.Domain.Entities.Orders;

/// <summary>
/// История применения транзакции к заказу.
/// </summary>
public class OrderTransactionHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid OrderTransactionId { get; set; }
    public OrderTransaction? OrderTransaction { get; set; }

    public DateTime PerformedAtUtc { get; set; }

    /// <summary>Id пользователя, выполнившего транзакцию (null — робот/система).</summary>
    public Guid? PerformedByUserId { get; set; }

    /// <summary>Имя пользователя для отображения (денормализовано).</summary>
    public string? PerformedByUserName { get; set; }

    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public int PricesUpdated { get; set; }
}
