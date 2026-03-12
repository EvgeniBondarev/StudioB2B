using Microsoft.AspNetCore.Http;

namespace StudioB2B.Infrastructure.Interfaces;

public interface ISubdomainResolver
{
    string? Resolve(HostString host);
}
