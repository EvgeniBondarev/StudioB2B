using AutoMapper;
using StudioB2B.Domain.Entities.Tenants;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Features.Roles;

/// <summary>
/// AutoMapper profile for Role mappings
/// </summary>
public class RoleMappingProfile : Profile
{
    public RoleMappingProfile()
    {
        // MasterRole → RoleDto (record with positional constructor)
        CreateMap<MasterRole, RoleDto>()
            .ConstructUsing(src => new RoleDto(src.Id, src.Name, src.Description, src.IsSystemRole, src.CreatedAtUtc));

        // CreateRoleRequest → MasterRole
        CreateMap<CreateRoleRequest, MasterRole>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.NormalizedName, opt => opt.MapFrom(src => src.Name.Trim().ToUpperInvariant()))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description == null ? null : src.Description.Trim()))
            .ForMember(dest => dest.ConcurrencyStamp, opt => opt.MapFrom(_ => Guid.NewGuid().ToString()))
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.Ignore());

        // UpdateRoleRequest → MasterRole (for updating existing entity)
        CreateMap<UpdateRoleRequest, MasterRole>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.NormalizedName, opt => opt.MapFrom(src => src.Name.Trim().ToUpperInvariant()))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description == null ? null : src.Description.Trim()))
            .ForMember(dest => dest.ConcurrencyStamp, opt => opt.MapFrom(_ => Guid.NewGuid().ToString()))
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.Ignore());
    }
}

