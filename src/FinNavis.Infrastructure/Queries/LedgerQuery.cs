using FinNavis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinNavis.Infrastructure.Queries;

internal static class LedgerQuery
{
    public static async Task<TResult> ReadAsync<TResult>(
        FinNavisDbContext context,
        string sql,
        Action<NpgsqlParameterCollection> bindParameters,
        Func<NpgsqlDataReader, CancellationToken, Task<TResult>> readResult,
        CancellationToken cancellationToken)
    {
        await context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = new NpgsqlCommand(
                sql,
                (NpgsqlConnection)context.Database.GetDbConnection());

            bindParameters(command.Parameters);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            return await readResult(reader, cancellationToken);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }
}
