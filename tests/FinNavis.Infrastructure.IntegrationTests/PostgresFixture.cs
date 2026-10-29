using FinNavis.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Npgsql;

using Testcontainers.PostgreSql;

namespace FinNavis.Infrastructure.IntegrationTests;

/// <summary>
/// Starts one PostgreSQL 18 container for the whole test run and hands out its connection string.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private const string PostgresImage = "postgres:18-alpine";

    // Written by EF Core, not by a test. Truncating it would make the fixture re-run migrations
    // that are already applied to the live schema.
    private const string MigrationsHistoryTable = "__EFMigrationsHistory";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(PostgresImage).Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Every later integration test starts from the real schema instead of one a test built
        // by hand, so a broken migration fails here rather than in whatever test runs first.
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public FinNavisDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinNavisDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new FinNavisDbContext(options);
    }

    public async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        return connection;
    }

    /// <summary>
    /// Empties every table so the next test cannot read rows another test wrote.
    /// </summary>
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);

        var tables = await ReadPublicTableNamesAsync(connection, cancellationToken);
        if (tables.Count == 0)
        {
            return;
        }

        var quoted = tables.Select(table => $"public.\"{table}\"");

        await using var truncate = new NpgsqlCommand(
            $"TRUNCATE TABLE {string.Join(", ", quoted)} RESTART IDENTITY CASCADE",
            connection);

        await truncate.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<List<string>> ReadPublicTableNamesAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT tablename FROM pg_tables WHERE schemaname = 'public' AND tablename <> @history",
            connection);

        command.Parameters.AddWithValue("history", MigrationsHistoryTable);

        var tables = new List<string>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }
}
