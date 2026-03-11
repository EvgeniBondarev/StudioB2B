using AutoMapper;
using StudioB2B.Domain.Entities.Master;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Features.Roles;

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

        // CreateRoleRequest → MasterRole
        CreateMap<CreateRoleRequest, MasterRole>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.UserRoles, opt => opt.Ignore());

        // UpdateRoleRequest → MasterRole
        CreateMap<UpdateRoleRequest, MasterRole>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.UserRoles, opt => opt.Ignore());
    }
}
