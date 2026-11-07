using Npgsql;

namespace FinNavis.Infrastructure.IntegrationTests.Repositories;

internal static class QueryPlan
{
    // A test table holds a handful of rows, and on so few rows the planner rightly prefers a
    // sequential scan whatever indexes exist. With sequential scans turned off, the plan shows
    // which index the query can use on a table of real size, and falls back to another path
    // only when that index is gone.
    public static async Task<string> ExplainAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        RecordedCommand command)
    {
        await using (var disableSequentialScan = new NpgsqlCommand(
            "SET LOCAL enable_seqscan = off", connection, transaction))
        {
            await disableSequentialScan.ExecuteNonQueryAsync();
        }

        await using var explain = new NpgsqlCommand($"EXPLAIN {command.Text}", connection, transaction);

        foreach (KeyValuePair<string, object?> parameter in command.Parameters)
        {
            explain.Parameters.Add(new NpgsqlParameter
            {
                ParameterName = parameter.Key,
                Value = parameter.Value ?? DBNull.Value,
            });
        }

        List<string> lines = [];

        await using var reader = await explain.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lines.Add(reader.GetString(0));
        }

        return string.Join(Environment.NewLine, lines);
    }
}
