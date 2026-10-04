using FinNavis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace FinNavis.Infrastructure.IntegrationTests;

/// <summary>
/// Starts one shared PostgreSQL container for every test class in the "Database" collection,
/// applies the real migration once, and lets each test class reset the table before it runs.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    // Same major as docker-compose.yml, so the schema is proved against the version the app
    // actually runs on.
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public FinNavisDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinNavisDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new FinNavisDbContext(options);
    }

    /// <summary>Clears the accounts table between tests, so one test class does not see another's data.</summary>
    public async Task ResetAsync()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE accounts RESTART IDENTITY CASCADE");
    }
}
