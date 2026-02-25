using AutoMapper;
using StudioB2B.Domain.Entities.Marketplace;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Profiles;

public class MarketplaceMappingProfile : Profile
{
    public MarketplaceMappingProfile()
    {
        CreateMap<MarketplaceClient, MarketplaceClientDto>()
            .ForMember(d => d.ClientTypeName, o => o.MapFrom(s => s.ClientType != null ? s.ClientType.Name : null))
            .ForMember(d => d.ModeName, o => o.MapFrom(s => s.Mode != null ? s.Mode.Name : null));

        CreateMap<MarketplaceClientType, MarketplaceClientTypeDto>();
        CreateMap<MarketplaceClientTypeDto, MarketplaceClientType>();

        CreateMap<MarketplaceClientMode, MarketplaceClientModeDto>();
        CreateMap<MarketplaceClientModeDto, MarketplaceClientMode>();

        CreateMap<MarketplaceClientSettings, MarketplaceClientSettingsDto>();
        CreateMap<MarketplaceClientSettingsDto, MarketplaceClientSettings>();

        CreateMap<MarketplaceClient1CSettings, MarketplaceClient1CSettingsDto>();
        CreateMap<MarketplaceClient1CSettingsDto, MarketplaceClient1CSettings>();
    }
}
