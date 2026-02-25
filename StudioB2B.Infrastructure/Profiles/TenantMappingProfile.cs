using AutoMapper;
using StudioB2B.Domain.Entities.Tenants;
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
