using AutoMapper;
using StudioB2B.Domain.Entities.Master;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Profiles;

public class MasterUserMappingProfile : Profile
{
    public MasterUserMappingProfile()
    {
        CreateMap<MasterUser, UserDto>();
        CreateMap<UserDto, MasterUser>();
    }
}
