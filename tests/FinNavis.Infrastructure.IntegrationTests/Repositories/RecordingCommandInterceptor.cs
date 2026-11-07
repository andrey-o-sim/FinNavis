using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FinNavis.Infrastructure.IntegrationTests.Repositories;

internal sealed class RecordingCommandInterceptor : DbCommandInterceptor
{
    private readonly List<RecordedCommand> _recorded = [];

    public IReadOnlyList<RecordedCommand> Recorded => _recorded;

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Record(command);

        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Record(command);

        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void Record(DbCommand command) =>
        _recorded.Add(new RecordedCommand(
            command.CommandText,
            [
                .. command.Parameters
                    .Cast<DbParameter>()
                    .Select(parameter => new KeyValuePair<string, object?>(
                        parameter.ParameterName,
                        parameter.Value)),
            ]));
}
