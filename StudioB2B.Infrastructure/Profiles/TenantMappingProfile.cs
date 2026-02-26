using AutoMapper;
using StudioB2B.Domain.Entities.Master;
using StudioB2B.Domain.Entities.Tenant;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Profiles;

public class TenantMappingProfile : Profile
{
    public TenantMappingProfile()
    {
        CreateMap<TenantEntity, TenantDto>();
        CreateMap<TenantDto, TenantEntity>();
    }
}
