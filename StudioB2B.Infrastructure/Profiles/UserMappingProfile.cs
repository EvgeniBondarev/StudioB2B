using AutoMapper;
using StudioB2B.Domain.Entities;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Profiles;

/// <summary>
/// AutoMapper profile for User mappings
/// </summary>
public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // TenantUser → UserListDto (Permissions загружаются отдельно в feature)
        CreateMap<TenantUser, UserListDto>()
            .ConstructUsing(src => new UserListDto(
                src.Id,
                src.Email,
                src.FirstName,
                src.LastName,
                src.MiddleName,
                src.IsActive,
                new List<string>()));

        // CreateUserDto → TenantUser
        CreateMap<CreateUserDto, TenantUser>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Trim().ToLowerInvariant()))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName.Trim()))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName.Trim()))
            .ForMember(dest => dest.MiddleName, opt => opt.MapFrom(src => src.MiddleName == null ? null : src.MiddleName.Trim()))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
            .ForMember(dest => dest.HashPassword, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.UserPermissions, opt => opt.Ignore());

        // UpdateUserDto → TenantUser (patch)
        CreateMap<UpdateUserDto, TenantUser>()
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName.Trim()))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName.Trim()))
            .ForMember(dest => dest.MiddleName, opt => opt.MapFrom(src => src.MiddleName == null ? null : src.MiddleName.Trim()))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.Ignore())
            .ForMember(dest => dest.HashPassword, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.UserPermissions, opt => opt.Ignore());
    }
}
