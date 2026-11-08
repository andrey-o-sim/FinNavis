using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FinNavis.Infrastructure.IntegrationTests.Endpoints.ImportRuns;

// Fails the load after its first insert has already reached the database, so the test proves the
// commit is rolled back as a whole rather than never started.
internal sealed class FailingInsertInterceptor : DbCommandInterceptor
{
    private int _insertsExecuted;

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        bool isInsert = command.CommandText.Contains("INSERT INTO", StringComparison.Ordinal);

        if (isInsert && Interlocked.Increment(ref _insertsExecuted) == 2)
        {
            await result.DisposeAsync();

            throw new InvalidOperationException("Test fault after the first insert of the load.");
        }

        return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
}
