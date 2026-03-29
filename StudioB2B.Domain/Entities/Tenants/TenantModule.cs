namespace StudioB2B.Domain.Entities;

/// <summary>Модуль, подключаемый на уровне тенанта.</summary>
public class TenantModule : IBaseEntity
{
    public Guid     Id          { get; set; }

    /// <summary>Уникальный код модуля (см. <see cref="StudioB2B.Domain.Constants.ModuleCodes"/>).</summary>
    public string   Code        { get; set; } = string.Empty;

    /// <summary>Отображаемое название.</summary>
    public string   Name        { get; set; } = string.Empty;

    /// <summary>Описание модуля.</summary>
    public string?  Description { get; set; }

    public bool     IsEnabled   { get; set; }
    public DateTime? EnabledAt  { get; set; }
    public DateTime? DisabledAt { get; set; }
}
