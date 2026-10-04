namespace FinNavis.Infrastructure.IntegrationTests;

/// <summary>
/// Every test class that needs the database carries <c>[Collection("Database")]</c>, so
/// Docker starts the PostgreSQL container once and the classes do not race each other for it.
/// </summary>
[CollectionDefinition("Database")]
public sealed class DatabaseCollection : ICollectionFixture<PostgresFixture>;
