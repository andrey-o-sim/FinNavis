using Npgsql;

namespace FinNavis.Infrastructure.IntegrationTests.Seeding;

// Counts rows with plain SQL, around EF Core, so a check that nothing was left behind cannot be
// satisfied by a change tracker or a query filter that hides a row.
internal static class TableRows
{
    public static readonly string[] ImportTables =
    [
        "import_runs", "import_transaction_links", "import_transfer_links", "import_created_account_links",
        "import_created_category_links", "import_set_aside_rows",
    ];

    public static async Task<long> CountAsync(PostgresFixture database, string table)
    {
        await using NpgsqlConnection connection = await database.OpenConnectionAsync();
        await using NpgsqlCommand count = new($"SELECT count(*) FROM {table}", connection);

        return (long)(await count.ExecuteScalarAsync())!;
    }
}
