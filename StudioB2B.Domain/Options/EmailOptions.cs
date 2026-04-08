namespace StudioB2B.Domain.Options;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = "";

    public int Port { get; set; } = 587;

    public string User { get; set; } = "";

    public string Password { get; set; } = "";

    public string FromAddress { get; set; } = "";

    public string FromName { get; set; } = "StudioB2B";

    public bool EnableSsl { get; set; } = true;
}

