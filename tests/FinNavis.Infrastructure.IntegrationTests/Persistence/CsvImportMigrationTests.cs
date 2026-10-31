using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using Npgsql;

namespace FinNavis.Infrastructure.IntegrationTests.Persistence;

[Collection(DatabaseCollection.Name)]
public sealed class CsvImportMigrationTests
{
    private const string MigrationBeforeCsvImport = "CoreLedgerInitial";

    private static readonly string[] ImportTables =
    [
        "mapping_profiles",
        "mapping_profile_rules",
        "import_runs",
        "import_transaction_links",
        "import_transfer_links",
        "import_created_account_links",
        "import_created_category_links",
        "import_set_aside_rows",
    ];

    private static readonly string[] LedgerTables =
        ["accounts", "category_groups", "categories", "transactions", "transfers"];

    private readonly PostgresFixture _database;

    public CsvImportMigrationTests(PostgresFixture database) => _database = database;

    [Fact]
    public async Task Migrate_ToLatest_CreatesTheEightImportTables()
    {
        await MigrateToAsync(targetMigration: null);

        var tables = await ReadPublicTableNamesAsync();

        tables.Should().Contain(ImportTables);
    }

    [Fact]
    public async Task Migrate_BackToCoreLedgerInitial_DropsTheImportTablesAndKeepsTheLedgerTables()
    {
        List<string> tables;
        try
        {
            await MigrateToAsync(MigrationBeforeCsvImport);
            tables = await ReadPublicTableNamesAsync();
        }
        finally
        {
            // The fixture is shared by the whole collection, so every later test expects the latest schema.
            await MigrateToAsync(targetMigration: null);
        }

        tables.Should().NotContain(ImportTables);
        tables.Should().Contain(LedgerTables);
    }

    private async Task MigrateToAsync(string? targetMigration)
    {
        await using var context = _database.CreateDbContext();
        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync(targetMigration);
    }

    private async Task<List<string>> ReadPublicTableNamesAsync()
    {
        await using var connection = await _database.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            "SELECT tablename FROM pg_tables WHERE schemaname = 'public'",
            connection);

        List<string> tables = [];

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }
}
