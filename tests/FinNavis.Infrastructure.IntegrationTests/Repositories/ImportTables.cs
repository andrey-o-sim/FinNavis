using Npgsql;

namespace FinNavis.Infrastructure.IntegrationTests.Repositories;

// Counts rows with plain SQL, around EF Core, so a check that something is gone cannot be
// satisfied by a change tracker or a query filter that hides the row.
internal static class ImportTables
{
    public static async Task<long> CountAsync(
        PostgresFixture database,
        string table,
        string column,
        Guid value)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var count = new NpgsqlCommand(
            $"SELECT count(*) FROM {table} WHERE {column} = @value",
            connection);

        count.Parameters.AddWithValue("value", value);

        return (long)(await count.ExecuteScalarAsync())!;
    }
}
