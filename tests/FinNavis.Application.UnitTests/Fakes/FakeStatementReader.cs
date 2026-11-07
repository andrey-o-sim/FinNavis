using System.Runtime.CompilerServices;
using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;

namespace FinNavis.Application.UnitTests.Fakes;

internal sealed class FakeStatementReader(StatementHeader header, IReadOnlyList<StatementRow> rows) : IStatementReader
{
    public List<CancellationToken> TokensSeen { get; } = [];

    public List<ImportFileKind> KindsSeen { get; } = [];

    public Task<StatementHeader> ReadHeaderAsync(
        Stream statement,
        ImportFileKind kind,
        CancellationToken cancellationToken)
    {
        TokensSeen.Add(cancellationToken);
        KindsSeen.Add(kind);

        return Task.FromResult(header);
    }

    public async IAsyncEnumerable<StatementRow> ReadRowsAsync(
        Stream statement,
        ImportFileKind kind,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        TokensSeen.Add(cancellationToken);
        KindsSeen.Add(kind);

        foreach (StatementRow row in rows)
        {
            await Task.Yield();
            yield return row;
        }
    }
}
