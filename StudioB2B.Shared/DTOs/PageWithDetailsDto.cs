namespace StudioB2B.Shared.DTOs;

public record PageWithDetailsDto(int Id, string Name, string DisplayName, List<LabelValueDto> Columns, List<LabelValueDto> Functions);

