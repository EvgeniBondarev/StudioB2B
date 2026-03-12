namespace StudioB2B.Shared.DTOs;

/// <summary>
/// Результат проведения документа по заказу.
/// </summary>
public class TransactionApplyResultDto
{
    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public List<string> ValidationErrors { get; set; } = [];

    public int PricesUpdated { get; set; }

    public int FieldsUpdated { get; set; }
}

