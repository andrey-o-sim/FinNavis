using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;

namespace FinNavis.Infrastructure.Import;

public sealed class CsvStatementReader : IStatementReader
{
    public CsvStatementReader(CsvStatementReaderOptions options)
    {
        throw new NotImplementedException();
    }

    public Task<StatementHeader> ReadHeaderAsync(Stream statement, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<StatementRow> ReadRowsAsync(
        Stream statement,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
