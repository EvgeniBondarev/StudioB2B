using AutoMapper;
using StudioB2B.Domain.Entities.Tenant;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Profiles;

public class TenantRoleMappingProfile : Profile
{
    public TenantRoleMappingProfile()
    {
        CreateMap<TenantRole, RoleDto>();
        CreateMap<RoleDto, TenantRole>();
    }
}
