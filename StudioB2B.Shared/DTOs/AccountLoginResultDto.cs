namespace StudioB2B.Shared;

public class AccountLoginResultDto
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public bool IsFullAccess { get; set; }

    public IEnumerable<string> RoleNames { get; set; } = [];
}

