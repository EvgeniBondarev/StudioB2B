using AutoMapper;
using StudioB2B.Domain.Entities;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Profiles;

/// <summary>
/// AutoMapper profile for Role mappings
/// </summary>
public class RoleMappingProfile : Profile
{
    public RoleMappingProfile()
    {
        // MasterRole → RoleDto
        CreateMap<MasterRole, RoleDto>()
            .ConstructUsing(src => new RoleDto(src.Id, src.Name));

        // CreateRoleDto → MasterRole
        CreateMap<CreateRoleDto, MasterRole>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.UserRoles, opt => opt.Ignore());

        // UpdateRoleDto → MasterRole
        CreateMap<UpdateRoleDto, MasterRole>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.UserRoles, opt => opt.Ignore());
    }
}
