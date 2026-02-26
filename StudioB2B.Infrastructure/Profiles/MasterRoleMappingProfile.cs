using AutoMapper;
using StudioB2B.Domain.Entities.Master;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Profiles;

public class MasterRoleMappingProfile : Profile
{
    public MasterRoleMappingProfile()
    {
        CreateMap<MasterRole, RoleDto>();
        CreateMap<RoleDto, MasterRole>();
    }
}
