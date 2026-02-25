namespace StudioB2B.Shared.DTOs;

public record UserDto(Guid Id,
                      string Email,
                      string PasswordHash,
                      List<RoleDto> Roles);

