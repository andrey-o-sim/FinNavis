using Npgsql;

namespace FinNavis.Infrastructure.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public sealed class PostgresFixtureTests : IAsyncLifetime
{
    private const string ProbeTable = "isolation_probe";

    private readonly PostgresFixture _database;

    public PostgresFixtureTests(PostgresFixture database) => _database = database;

    public async Task InitializeAsync()
    {
        await _database.ResetAsync();

        await using var connection = await _database.OpenConnectionAsync();
        await using var create = new NpgsqlCommand(
            $"CREATE TABLE IF NOT EXISTS {ProbeTable} (value text NOT NULL)",
            connection);

        await create.ExecuteNonQueryAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task OpenConnectionAsync_WithTheFixtureStarted_ReachesPostgres18()
    {
        await using var connection = await _database.OpenConnectionAsync();

        connection.PostgreSqlVersion.Major.Should().Be(18);
    }

    // Two cases, run one after the other against the same table. Without the reset the second
    // case would read the row the first one wrote, so this fails loudly if isolation breaks.
    [Theory]
    [InlineData("first")]
    [InlineData("second")]
    public async Task ResetAsync_BetweenTwoTestsWritingTheSameTable_LeavesOnlyTheOwnRow(string value)
    {
        await using var connection = await _database.OpenConnectionAsync();

        await using var insert = new NpgsqlCommand($"INSERT INTO {ProbeTable} (value) VALUES (@value)", connection);
        insert.Parameters.AddWithValue("value", value);
        await insert.ExecuteNonQueryAsync();

        await using var read = new NpgsqlCommand($"SELECT value FROM {ProbeTable}", connection);
        await using var reader = await read.ExecuteReaderAsync();

        var rows = new List<string>();
        while (await reader.ReadAsync())
        {
            rows.Add(reader.GetString(0));
        }

        rows.Should().ContainSingle().Which.Should().Be(value);
    }
}
