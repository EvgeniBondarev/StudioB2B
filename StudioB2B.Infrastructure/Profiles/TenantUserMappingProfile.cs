using AutoMapper;
using StudioB2B.Domain.Entities.Tenant;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Profiles;

public class TenantUserMappingProfile : Profile
{
    public TenantUserMappingProfile()
    {
        CreateMap<TenantUser, TenantUserDto>();
        CreateMap<TenantUserDto, TenantUser>();
    }
}

