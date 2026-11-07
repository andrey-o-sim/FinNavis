using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text;
using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;

namespace FinNavis.Infrastructure.Import;

public sealed class CsvStatementReader(CsvStatementReaderOptions options) : IStatementReader
{
    private const string ShapeKeySeparator = "|";
    private const int HeaderLineCount = 1;

    private static readonly Dictionary<StatementColumn, string> RecognisedColumnNames = new()
    {
        [StatementColumn.Date] = "date",
        [StatementColumn.Amount] = "amount",
        [StatementColumn.Account] = "account",
    };

    public async Task<StatementHeader> ReadHeaderAsync(
        Stream statement,
        ImportFileKind kind,
        CancellationToken cancellationToken)
    {
        using StreamReader reader = OpenReader(statement);
        string headerLine = await reader.ReadLineAsync(cancellationToken) ?? string.Empty;

        List<string> columnNames = [.. CsvLineSplitter.Split(headerLine).Select(name => name.Trim())];
        List<string> normalisedNames = NormaliseColumnNames(columnNames);

        return new StatementHeader(
            columnNames,
            string.Join(ShapeKeySeparator, normalisedNames),
            FindMissingColumns(normalisedNames),
            ReadOnlyDictionary<StatementColumn, int>.Empty);
    }

    public async IAsyncEnumerable<StatementRow> ReadRowsAsync(
        Stream statement,
        ImportFileKind kind,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using StreamReader reader = OpenReader(statement);
        await reader.ReadLineAsync(cancellationToken);

        int rowsRead = 0;

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            // ReadLineAsync looks at the token only when it has to pull more bytes from the stream,
            // so a cancel that lands while the next lines are already buffered would go unnoticed.
            cancellationToken.ThrowIfCancellationRequested();
            rowsRead++;
            EnsureWithinRowCeiling(rowsRead);

            yield return new StatementRow(HeaderLineCount + rowsRead, CsvLineSplitter.Split(line), line);
        }
    }

    private static StreamReader OpenReader(Stream statement)
    {
        return new StreamReader(statement, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
    }

    private static List<string> NormaliseColumnNames(IEnumerable<string> columnNames)
    {
        List<string> normalisedNames = [.. columnNames.Select(name => name.ToLowerInvariant())];
        while (normalisedNames.Count > 0 && normalisedNames[^1].Length == 0)
        {
            normalisedNames.RemoveAt(normalisedNames.Count - 1);
        }

        return normalisedNames;
    }

    private static HashSet<StatementColumn> FindMissingColumns(List<string> normalisedNames)
    {
        return
        [
            .. RecognisedColumnNames
                .Where(recognised => !normalisedNames.Contains(recognised.Value))
                .Select(recognised => recognised.Key),
        ];
    }

    private void EnsureWithinRowCeiling(int rowsRead)
    {
        if (rowsRead > options.RowCeiling)
        {
            throw new InvalidDataException(
                $"The file has more than {options.RowCeiling} rows, which is more than a statement can hold.");
        }
    }
}
