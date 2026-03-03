using AutoMapper;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Features.Users;

/// <summary>
/// AutoMapper profile for User mappings
/// </summary>
public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // ApplicationUser → UserListDto (record with positional constructor; Roles loaded separately)
        CreateMap<ApplicationUser, UserListDto>()
            .ConstructUsing(src => new UserListDto(
                src.Id,
                src.Email ?? string.Empty,
                src.FirstName,
                src.LastName,
                src.MiddleName,
                src.IsActive,
                src.CreatedAtUtc,
                src.LastLoginAtUtc,
                new List<string>()));

        // CreateUserRequest → ApplicationUser
        CreateMap<CreateUserRequest, ApplicationUser>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email.Trim().ToLowerInvariant()))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Trim().ToLowerInvariant()))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName.Trim()))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName.Trim()))
            .ForMember(dest => dest.MiddleName, opt => opt.MapFrom(src => src.MiddleName == null ? null : src.MiddleName.Trim()))
            .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(_ => true))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true));

        // UpdateUserRequest → ApplicationUser (patch existing entity)
        CreateMap<UpdateUserRequest, ApplicationUser>()
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName.Trim()))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName.Trim()))
            .ForMember(dest => dest.MiddleName, opt => opt.MapFrom(src => src.MiddleName == null ? null : src.MiddleName.Trim()))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserName, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.Ignore())
            .ForMember(dest => dest.EmailConfirmed, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.LastLoginAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.NormalizedUserName, opt => opt.Ignore())
            .ForMember(dest => dest.NormalizedEmail, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
            .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore())
            .ForMember(dest => dest.PhoneNumber, opt => opt.Ignore())
            .ForMember(dest => dest.PhoneNumberConfirmed, opt => opt.Ignore())
            .ForMember(dest => dest.TwoFactorEnabled, opt => opt.Ignore())
            .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
            .ForMember(dest => dest.LockoutEnabled, opt => opt.Ignore())
            .ForMember(dest => dest.AccessFailedCount, opt => opt.Ignore());
    }
}

