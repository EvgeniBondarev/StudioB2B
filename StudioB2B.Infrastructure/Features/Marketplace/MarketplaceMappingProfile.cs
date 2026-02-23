using AutoMapper;
using StudioB2B.Domain.Entities.Marketplace;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Features.Marketplace;

public class MarketplaceMappingProfile : Profile
{
    public MarketplaceMappingProfile()
    {
        // entity -> dto
        CreateMap<MarketplaceClient, MarketplaceClientDto>()
            .ForMember(d => d.ClientTypeName, o => o.MapFrom(s => s.ClientType != null ? s.ClientType.Name : null))
            .ForMember(d => d.ModeName, o => o.MapFrom(s => s.Mode != null ? s.Mode.Name : null));

        CreateMap<MarketplaceClientType, MarketplaceClientTypeDto>();
        CreateMap<MarketplaceClientMode, MarketplaceClientModeDto>();
        CreateMap<MarketplaceClientSettings, MarketplaceClientSettingDto>();
        CreateMap<MarketplaceClient1CSettings, MarketplaceClient1CSettingsDto>();

        // request -> entity
        CreateMap<CreateMarketplaceClientRequest, MarketplaceClient>();
        CreateMap<UpdateMarketplaceClientRequest, MarketplaceClient>();
    }
}
