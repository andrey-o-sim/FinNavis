using Npgsql;

namespace FinNavis.Infrastructure.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public sealed class CoreLedgerMigrationTests
{
    private readonly PostgresFixture _database;

    public CoreLedgerMigrationTests(PostgresFixture database) => _database = database;

    [Fact]
    public async Task Migrate_RunByTheFixture_CreatesTheFiveLedgerTables()
    {
        var tables = await ReadPublicTableNamesAsync();

        tables.Should().Contain(["accounts", "category_groups", "categories", "transactions", "transfers"]);
    }

    private async Task<List<string>> ReadPublicTableNamesAsync()
    {
        await using var connection = await _database.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            "SELECT tablename FROM pg_tables WHERE schemaname = 'public'",
            connection);

        var tables = new List<string>();

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }
}
