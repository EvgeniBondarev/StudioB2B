using Xunit;

namespace StudioB2B.Tests.Integration;

/// <summary>
/// Marks all integration test classes that share a single MySQL container instance.
/// </summary>
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<Database.TenantDbContextFixture>
{
}
