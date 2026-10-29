namespace FinNavis.Infrastructure.IntegrationTests;

/// <summary>
/// Every integration test joins this collection, so the container starts once per run
/// instead of once per test class.
/// </summary>
[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
