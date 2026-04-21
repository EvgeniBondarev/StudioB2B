namespace StudioB2B.Domain.Entities;

public class EmailVerificationCode
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string Code { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; }
}
