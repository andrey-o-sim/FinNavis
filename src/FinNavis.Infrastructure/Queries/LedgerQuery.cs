using FinNavis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinNavis.Infrastructure.Queries;

/// <summary>
/// Runs one read statement on the context's own connection and hands the reader to the caller.
/// </summary>
/// <remarks>
/// The read path is written as SQL rather than as LINQ (ADR-0004): every figure the owner reads is
/// an aggregate, and the statement is the thing that has to stay readable next to the indexes it
/// relies on.
/// <para>
/// The connection is EF Core's, so a query joins whatever transaction the caller already opened
/// instead of taking a second connection. <c>OpenConnectionAsync</c> and
/// <c>CloseConnectionAsync</c> count their calls, so closing here cannot close a connection EF
/// opened for someone else.
/// </para>
/// </remarks>
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
