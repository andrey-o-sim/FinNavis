using System.Runtime.CompilerServices;
using System.Text;
using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;

namespace FinNavis.Infrastructure.Import;

public sealed class CsvStatementReader(CsvStatementReaderOptions options) : IStatementReader
{
    private const string ShapeKeySeparator = "|";
    private const char Comma = ',';
    private const char Tab = '\t';
    private const string ByteOrderMark = "\uFEFF";

    private static readonly StatementColumn[] RequiredColumns =
        [StatementColumn.Date, StatementColumn.Amount, StatementColumn.Account];

    public async Task<StatementHeader> ReadHeaderAsync(
        Stream statement,
        ImportFileKind kind,
        CancellationToken cancellationToken)
    {
        using StreamReader reader = OpenReader(statement);
        FileHeader fileHeader = await ReadFileHeaderAsync(reader, CsvFileLayout.For(kind), cancellationToken);

        return new StatementHeader(
            fileHeader.ColumnNames,
            string.Join(ShapeKeySeparator, fileHeader.NormalisedNames),
            FindMissingColumns(fileHeader.ColumnPositions),
            fileHeader.ColumnPositions);
    }

    public async IAsyncEnumerable<StatementRow> ReadRowsAsync(
        Stream statement,
        ImportFileKind kind,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        CsvFileLayout layout = CsvFileLayout.For(kind);
        using StreamReader reader = OpenReader(statement);
        FileHeader fileHeader = await ReadFileHeaderAsync(reader, layout, cancellationToken);

        int lineNumber = fileHeader.LineNumber;
        int rowsRead = 0;

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            // ReadLineAsync looks at the token only when it has to pull more bytes from the stream,
            // so a cancel that lands while the next lines are already buffered would go unnoticed.
            cancellationToken.ThrowIfCancellationRequested();
            lineNumber++;

            List<string> values = CsvLineSplitter.Split(line, fileHeader.Separator);
            if (layout.SkipsRowsWithNoDate && HasNoDate(values, fileHeader.ColumnPositions))
            {
                continue;
            }

            rowsRead++;
            EnsureWithinRowCeiling(rowsRead);

            yield return new StatementRow(lineNumber, values, line);
        }
    }

    private static StreamReader OpenReader(Stream statement)
    {
        return new StreamReader(statement, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
    }

    private static async Task<FileHeader> ReadFileHeaderAsync(
        StreamReader reader,
        CsvFileLayout layout,
        CancellationToken cancellationToken)
    {
        int lineNumber = 0;
        if (layout.OpensWithTitleLine)
        {
            await reader.ReadLineAsync(cancellationToken);
            lineNumber++;
        }

        string headerLine = await reader.ReadLineAsync(cancellationToken) ?? string.Empty;
        lineNumber++;

        char separator = headerLine.Contains(Tab) ? Tab : Comma;
        List<string> columnNames = [.. CsvLineSplitter.Split(headerLine, separator).Select(CleanColumnName)];
        List<string> normalisedNames = NormaliseColumnNames(columnNames);
        Dictionary<StatementColumn, int> columnPositions = FindColumnPositions(normalisedNames, layout);

        return new FileHeader(lineNumber, separator, columnNames, normalisedNames, columnPositions);
    }

    // The old tracker writes a stray byte order mark into its last column name.
    private static string CleanColumnName(string name)
    {
        return name.Replace(ByteOrderMark, string.Empty, StringComparison.Ordinal).Trim();
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

    private static Dictionary<StatementColumn, int> FindColumnPositions(
        List<string> normalisedNames,
        CsvFileLayout layout)
    {
        return layout.RecognisedColumnNames
            .Select(recognised => (Column: recognised.Key, Position: normalisedNames.IndexOf(recognised.Value)))
            .Where(found => found.Position >= 0)
            .ToDictionary(found => found.Column, found => found.Position);
    }

    private static HashSet<StatementColumn> FindMissingColumns(Dictionary<StatementColumn, int> columnPositions)
    {
        return [.. RequiredColumns.Where(column => !columnPositions.ContainsKey(column))];
    }

    private static bool HasNoDate(List<string> values, Dictionary<StatementColumn, int> columnPositions)
    {
        return columnPositions.TryGetValue(StatementColumn.Date, out int datePosition)
            && (datePosition >= values.Count || string.IsNullOrWhiteSpace(values[datePosition]));
    }

    private void EnsureWithinRowCeiling(int rowsRead)
    {
        if (rowsRead > options.RowCeiling)
        {
            throw new InvalidDataException(
                $"The file has more than {options.RowCeiling} rows, which is more than a statement can hold.");
        }
    }

    private sealed record FileHeader(
        int LineNumber,
        char Separator,
        List<string> ColumnNames,
        List<string> NormalisedNames,
        Dictionary<StatementColumn, int> ColumnPositions);
}
