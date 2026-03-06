namespace StudioB2B.Application.Common;

/// <summary>
/// Результат применения транзакции к заказу.
/// </summary>
public class TransactionApplyResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int PricesUpdated { get; set; }
}
