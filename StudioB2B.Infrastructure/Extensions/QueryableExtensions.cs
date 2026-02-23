using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace StudioB2B.Infrastructure.Extensions;

public static class QueryableExtensions
{
    /// <summary>
    /// Projects the queryable source to destination type using AutoMapper configuration.
    /// </summary>
    public static IQueryable<TDest> CastToDto<TSource, TDest>(
        this IQueryable<TSource> query,
        IConfigurationProvider config)
    {
        return query.ProjectTo<TDest>(config);
    }

    /// <summary>
    /// Convenience overload that accepts IMapper.
    /// </summary>
    public static IQueryable<TDest> CastToDto<TSource, TDest>(
        this IQueryable<TSource> query,
        IMapper mapper)
    {
        return query.ProjectTo<TDest>(mapper.ConfigurationProvider);
    }
}
