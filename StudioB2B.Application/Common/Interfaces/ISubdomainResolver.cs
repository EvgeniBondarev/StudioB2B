namespace StudioB2B.Application.Common.Interfaces;

public interface ISubdomainResolver
{
    string? Resolve(string host);
}
