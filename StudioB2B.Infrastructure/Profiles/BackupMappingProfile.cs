using AutoMapper;
using StudioB2B.Domain.Entities;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Profiles;

public class BackupMappingProfile : Profile
{
    public BackupMappingProfile()
    {
        CreateMap<TenantBackupSchedule, TenantBackupScheduleDto>();
        CreateMap<TenantBackupHistory, TenantBackupHistoryDto>();
        CreateMap<TenantRestoreHistory, TenantRestoreHistoryDto>();
    }
}

