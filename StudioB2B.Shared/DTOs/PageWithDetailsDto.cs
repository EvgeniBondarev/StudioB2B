namespace StudioB2B.Shared;

public record PageWithDetailsDto(int Id, string Name, string DisplayName, List<LabelValueDto> Columns, List<LabelValueDto> Functions);

