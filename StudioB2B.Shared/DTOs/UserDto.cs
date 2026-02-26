namespace StudioB2B.Shared.DTOs;

public record UserDto(Guid Id,
                      string Email,
                      string PasswordHash,
                      List<RoleDto> Roles);

public record TenantUserDto(Guid Id,
                             string Email,
                             string Surname,
                             string FirstName,
                             string Patronymic,
                             string PasswordHash,
                             List<RoleDto> Roles);

