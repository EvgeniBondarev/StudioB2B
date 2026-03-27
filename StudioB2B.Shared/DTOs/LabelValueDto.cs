namespace StudioB2B.Shared;

public class LabelValueDto
{
    public LabelValueDto(string label, string value) { Label = label; Value = value; }
    public string Label { get; set; }

    public string Value { get; set; }
}
