using AutoMapper;
using StudioB2B.Domain.Entities.Tenants;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Features.Users;

/// <summary>
/// AutoMapper profile for User mappings
/// </summary>
public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // User → UserListDto (Roles загружаются отдельно)
        CreateMap<User, UserListDto>()
            .ConstructUsing(src => new UserListDto(
                src.Id,
                src.Email,
                src.FirstName,
                src.LastName,
                src.MiddleName,
                src.IsActive,
                new List<string>()));

        // CreateUserRequest → User
        CreateMap<CreateUserRequest, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Trim().ToLowerInvariant()))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName.Trim()))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName.Trim()))
            .ForMember(dest => dest.MiddleName, opt => opt.MapFrom(src => src.MiddleName == null ? null : src.MiddleName.Trim()))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
            .ForMember(dest => dest.HashPassword, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.UserRoles, opt => opt.Ignore());

        // UpdateUserRequest → User (patch)
        CreateMap<UpdateUserRequest, User>()
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName.Trim()))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName.Trim()))
            .ForMember(dest => dest.MiddleName, opt => opt.MapFrom(src => src.MiddleName == null ? null : src.MiddleName.Trim()))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.Ignore())
            .ForMember(dest => dest.HashPassword, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.UserRoles, opt => opt.Ignore());
    }
}
