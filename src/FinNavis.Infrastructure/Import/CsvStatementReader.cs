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
    private const char ByteOrderMark = '﻿';

    private static readonly StatementColumn[] RequiredColumns =
        [StatementColumn.Date, StatementColumn.Amount, StatementColumn.Account];

    // The statement records the purchase amount, which the bank writes unsigned, and takes its sign
    // from the card amount. A statement covers one account, so it has no account column.
    private static readonly Dictionary<StatementColumn, string> StatementColumnNames = new()
    {
        [StatementColumn.Date] = "дата",
        [StatementColumn.AmountSign] = "сума в валюті картки",
        [StatementColumn.Amount] = "сума в валюті транзакції",
        [StatementColumn.Currency] = "валюта транзакції",
    };

    private static readonly Dictionary<StatementColumn, string> ExportColumnNames = new()
    {
        [StatementColumn.Date] = "date",
        [StatementColumn.Amount] = "amount",
        [StatementColumn.Currency] = "currency",
        [StatementColumn.Account] = "account",
    };

    public async Task<StatementHeader> ReadHeaderAsync(
        Stream statement,
        ImportFileKind kind,
        CancellationToken cancellationToken)
    {
        using StreamReader reader = OpenReader(statement);
        FileHeader fileHeader = await ReadFileHeaderAsync(reader, kind, cancellationToken);

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
        using StreamReader reader = OpenReader(statement);
        FileHeader fileHeader = await ReadFileHeaderAsync(reader, kind, cancellationToken);

        int lineNumber = fileHeader.LineNumber;
        int rowsRead = 0;

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            // ReadLineAsync looks at the token only when it has to pull more bytes from the stream,
            // so a cancel that lands while the next lines are already buffered would go unnoticed.
            cancellationToken.ThrowIfCancellationRequested();
            lineNumber++;

            List<string> values = CsvLineSplitter.Split(line, fileHeader.Separator);
            if (kind == ImportFileKind.Export && HasNoDate(values, fileHeader.ColumnPositions))
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

    // A statement opens with a title line that names the period. It is skipped unread, because its
    // content changes every month and the bank pads it with commas so it looks like a row.
    private static async Task<FileHeader> ReadFileHeaderAsync(
        StreamReader reader,
        ImportFileKind kind,
        CancellationToken cancellationToken)
    {
        int lineNumber = 0;
        if (kind == ImportFileKind.Statement)
        {
            await reader.ReadLineAsync(cancellationToken);
            lineNumber++;
        }

        string headerLine = await reader.ReadLineAsync(cancellationToken) ?? string.Empty;
        lineNumber++;

        char separator = headerLine.Contains(Tab) ? Tab : Comma;
        List<string> columnNames = [.. CsvLineSplitter.Split(headerLine, separator).Select(CleanColumnName)];
        List<string> normalisedNames = NormaliseColumnNames(columnNames);
        Dictionary<StatementColumn, int> columnPositions = FindColumnPositions(normalisedNames, kind);

        return new FileHeader(lineNumber, separator, columnNames, normalisedNames, columnPositions);
    }

    // The old tracker writes a stray byte order mark into its last column name.
    private static string CleanColumnName(string name)
    {
        return name.Replace(ByteOrderMark.ToString(), string.Empty, StringComparison.Ordinal).Trim();
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
        ImportFileKind kind)
    {
        Dictionary<StatementColumn, string> recognisedNames =
            kind == ImportFileKind.Statement ? StatementColumnNames : ExportColumnNames;

        return recognisedNames
            .Select(recognised => (Column: recognised.Key, Position: normalisedNames.IndexOf(recognised.Value)))
            .Where(found => found.Position >= 0)
            .ToDictionary(found => found.Column, found => found.Position);
    }

    private static HashSet<StatementColumn> FindMissingColumns(Dictionary<StatementColumn, int> columnPositions)
    {
        return [.. RequiredColumns.Where(column => !columnPositions.ContainsKey(column))];
    }

    // The export ends with the tracker's own totals, which carry no date. They are not movements.
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
