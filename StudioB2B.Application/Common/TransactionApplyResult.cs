namespace StudioB2B.Application.Common;

/// <summary>
/// Результат проведения документа по заказу.
/// </summary>
public class TransactionApplyResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> ValidationErrors { get; set; } = [];
    public int PricesUpdated { get; set; }
    public int FieldsUpdated { get; set; }
}
