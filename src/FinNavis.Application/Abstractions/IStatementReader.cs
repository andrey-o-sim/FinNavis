using FinNavis.Application.Abstractions.Reads;

namespace FinNavis.Application.Abstractions;

public interface IStatementReader
{
    Task<StatementHeader> ReadHeaderAsync(Stream statement, CancellationToken cancellationToken);

    // Each call reads the stream from its first line and skips the header, because the confirm step
    // and the load step each make their own pass over the file and a stream can be read only once.
    IAsyncEnumerable<StatementRow> ReadRowsAsync(
        Stream statement,
        CancellationToken cancellationToken);
}
