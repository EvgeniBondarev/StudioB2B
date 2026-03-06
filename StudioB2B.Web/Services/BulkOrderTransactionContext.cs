using System.Collections.ObjectModel;

namespace StudioB2B.Web.Services;

/// <summary>
/// Состояние выбора заказов для массового применения транзакций.
/// </summary>
public class BulkOrderTransactionContext
{
    private IReadOnlyList<Guid> _orderIds = Array.Empty<Guid>();

    public IReadOnlyList<Guid> OrderIds => _orderIds;

    public void SetSelection(IEnumerable<Guid> ids)
    {
        _orderIds = new ReadOnlyCollection<Guid>(ids.Distinct().ToList());
    }
}

