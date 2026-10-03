using FinNavis.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace FinNavis.Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// Boots the real API against a real PostgreSQL in a container.
/// </summary>
/// <remarks>
/// Shared by every test class in <see cref="ApiCollection"/>, so the container starts once per
/// assembly. The classes run one after another, because they share one database.
/// </remarks>
public sealed class FinNavisApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Same image as docker-compose.yml, so tests and local development run the same PostgreSQL.
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public async Task InitializeAsync()
    {
        // The container has to be running before anything touches Services: the first read of
        // Services builds the host, and the host needs the connection string.
        await _database.StartAsync();

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FinNavisDbContext>();

        // MigrateAsync, not EnsureCreatedAsync. EnsureCreated builds the schema from the model,
        // so it passes even when the migration is missing or wrong — which is the exact failure
        // this suite exists to catch.
        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Empties every table, so each test class starts from a known state.
    /// </summary>
    public async Task ResetAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FinNavisDbContext>();

        // Read the table names from the model, so this never goes stale when an entity is added.
        var tables = context.Model.GetEntityTypes()
            .Select(entity => entity.GetTableName())
            .Where(table => table is not null)
            .Distinct()
            .Select(table => $"\"{table}\"");

        var sql = $"TRUNCATE {string.Join(", ", tables)} RESTART IDENTITY CASCADE;";

        await context.Database.ExecuteSqlRawAsync(sql);
    }

    /// <summary>
    /// Explicit implementation on purpose. WebApplicationFactory already has a DisposeAsync
    /// with the same name and parameters but a different return type, and the two collide.
    /// </summary>
    async Task IAsyncLifetime.DisposeAsync()
    {
        await _database.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:FinNavisDb"] = _database.GetConnectionString(),
            });
        });
    }
}
