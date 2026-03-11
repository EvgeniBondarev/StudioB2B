using System.ComponentModel.DataAnnotations;

namespace StudioB2B.Domain.Entities;

/// <summary>
/// Валюта (RUB, USD, EUR и т.д.).
/// </summary>
[Display(Name = "Валюта")]
public class Currency : IBaseEntity, ISoftDelete
{
    [Display(Name = "Идентификатор валюты")]
    public Guid Id { get; set; }

    /// <summary>ISO-код валюты (RUB, USD, EUR).</summary>
    [Display(Name = "Код валюты")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "Наименование валюты")]
    public string? Name { get; set; }

    public bool IsDeleted { get; set; }
}
