namespace StudioB2B.Shared;

/// <summary>Одна строка расшифровки: какое правило тарифа дало какую сумму.</summary>
public class PaymentBreakdownLineDto
{
    public string Caption { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}
