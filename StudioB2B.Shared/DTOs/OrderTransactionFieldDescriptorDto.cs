using StudioB2B.Domain.Constants;

namespace StudioB2B.Shared;

/// <summary>
/// Описание поля, доступного для изменения в документе.
/// </summary>
public record OrderTransactionFieldDescriptorDto(
    string EntityPath,
    string DisplayName,
    TransactionFieldValueTypeEnum ValueType,
    FieldReferenceTypeEnum ReferenceType = FieldReferenceTypeEnum.None);

