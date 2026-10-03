namespace FinNavis.Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// Every test class that touches the database belongs to this collection.
/// </summary>
/// <remarks>
/// Two reasons. The container starts once for the whole assembly instead of once per class,
/// and the classes run one after another rather than in parallel. They share one database,
/// so a class that forgets <c>[Collection(ApiCollection.Name)]</c> would truncate the table
/// under the others, and the failure would look like a race condition.
/// </remarks>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<FinNavisApiFactory>
{
    public const string Name = "FinNavis API";
}
