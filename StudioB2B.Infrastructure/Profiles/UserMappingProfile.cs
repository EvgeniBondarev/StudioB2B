using AutoMapper;
using StudioB2B.Domain.Entities.Common;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Profiles;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>();
        CreateMap<UserDto, User>();
    }
}

