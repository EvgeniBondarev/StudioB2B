using Microsoft.AspNetCore.Http;

namespace StudioB2B.Infrastructure.MultiTenancy.Resolution;

public interface ISubdomainResolver
{
    string? Resolve(HostString host);
}
