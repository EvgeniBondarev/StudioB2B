using System.ComponentModel.DataAnnotations;

namespace StudioB2B.Domain.Constants;

/// <summary>
/// Статусы отправления Ozon в порядке жизненного цикла.
/// </summary>
public enum OzonShipmentStatusEnum
{
    [Display(Name = "Неизвестно")]
    Unknown = 0,

    [Display(Name = "Создано")]
    AwaitingVerification = 1,

    [Display(Name = "Ожидает регистрации")]
    AwaitingRegistration = 2,

    [Display(Name = "Идёт приёмка")]
    AcceptanceInProgress = 3,

    [Display(Name = "Ожидает упаковки")]
    AwaitingPackaging = 4,

    [Display(Name = "Ожидает отгрузки")]
    AwaitingDeliver = 5,

    [Display(Name = "У водителя")]
    DriverPickup = 6,

    [Display(Name = "Не принят на СЦ")]
    NotAccepted = 7,

    [Display(Name = "Доставляется")]
    Delivering = 8,

    [Display(Name = "Арбитраж")]
    ClientArbitration = 9,

    [Display(Name = "Доставлено")]
    Delivered = 10,

    [Display(Name = "Отменено")]
    Cancelled = 11,

    [Display(Name = "Отменён (разделение)")]
    CancelledFromSplitPending = 12,

    [Display(Name = "Ожидает подтверждения")]
    AwaitingApprove = 13,
}

public static class OzonShipmentStatusExtensions
{
    private static readonly Dictionary<string, OzonShipmentStatusEnum> SynonymMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["awaiting_verification"] = OzonShipmentStatusEnum.AwaitingVerification,
        ["awaiting_registration"] = OzonShipmentStatusEnum.AwaitingRegistration,
        ["acceptance_in_progress"] = OzonShipmentStatusEnum.AcceptanceInProgress,
        ["awaiting_packaging"] = OzonShipmentStatusEnum.AwaitingPackaging,
        ["awaiting_deliver"] = OzonShipmentStatusEnum.AwaitingDeliver,
        ["driver_pickup"] = OzonShipmentStatusEnum.DriverPickup,
        ["not_accepted"] = OzonShipmentStatusEnum.NotAccepted,
        ["delivering"] = OzonShipmentStatusEnum.Delivering,
        ["client_arbitration"] = OzonShipmentStatusEnum.ClientArbitration,
        ["delivered"] = OzonShipmentStatusEnum.Delivered,
        ["cancelled"] = OzonShipmentStatusEnum.Cancelled,
        ["cancelled_from_split_pending"] = OzonShipmentStatusEnum.CancelledFromSplitPending,
        ["awaiting_approve"] = OzonShipmentStatusEnum.AwaitingApprove,
    };

    /// <summary>
    /// Статусы, при которых отправление ещё НЕ передано перевозчику (до отгрузки).
    /// </summary>
    public static bool IsBeforeShipment(this OzonShipmentStatusEnum status) =>
        status < OzonShipmentStatusEnum.AwaitingDeliver;

    public static OzonShipmentStatusEnum FromSynonym(string? synonym) =>
        synonym is not null && SynonymMap.TryGetValue(synonym, out var s) ? s : OzonShipmentStatusEnum.Unknown;
}
