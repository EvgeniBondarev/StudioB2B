namespace StudioB2B.Shared;

public record ChangeUserPasswordDto(
    Guid UserId,
    string NewPassword);

