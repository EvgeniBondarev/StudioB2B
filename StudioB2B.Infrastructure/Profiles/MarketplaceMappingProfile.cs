using AutoMapper;
using StudioB2B.Domain.Entities;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Profiles;

public class MarketplaceMappingProfile : Profile
{
    public MarketplaceMappingProfile()
    {
        CreateMap<MarketplaceClient, MarketplaceClientDto>()
            .ForMember(d => d.ClientTypeName, o => o.MapFrom(s => s.ClientType != null ? s.ClientType.Name : null))
            .ForMember(d => d.ModeIds, o => o.MapFrom(s =>
                new List<Guid?>
                {
                    s.ModeId,
                    s.ModeId2
                }
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList()))
            .ForMember(d => d.ModeNames, o => o.MapFrom(s =>
                new List<string?>
                {
                    s.Mode != null ? s.Mode.Name : null,
                    s.Mode2 != null ? s.Mode2.Name : null
                }
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n!)
                .ToList()));

        CreateMap<MarketplaceClientType, MarketplaceClientTypeDto>();
        CreateMap<MarketplaceClientMode, MarketplaceClientModeDto>();
        CreateMap<MarketplaceClientSettings, MarketplaceClientSettingDto>();
        CreateMap<MarketplaceClient1CSettings, MarketplaceClient1CSettingsDto>();
        CreateMap<CreateMarketplaceClientDto, MarketplaceClient>()
            .ForMember(d => d.ModeId, o => o.MapFrom(s => s.ModeIds != null && s.ModeIds.Count > 0 ? s.ModeIds[0] : (Guid?)null))
            .ForMember(d => d.ModeId2, o => o.MapFrom(s => s.ModeIds != null && s.ModeIds.Count > 1 ? s.ModeIds[1] : (Guid?)null));
        CreateMap<UpdateMarketplaceClientDto, MarketplaceClient>()
            .ForMember(d => d.ModeId, o => o.MapFrom(s => s.ModeIds != null && s.ModeIds.Count > 0 ? s.ModeIds[0] : (Guid?)null))
            .ForMember(d => d.ModeId2, o => o.MapFrom(s => s.ModeIds != null && s.ModeIds.Count > 1 ? s.ModeIds[1] : (Guid?)null));
    }
}
