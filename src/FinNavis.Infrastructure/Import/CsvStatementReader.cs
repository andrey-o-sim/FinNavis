using System.Runtime.CompilerServices;
using System.Text;
using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;

namespace FinNavis.Infrastructure.Import;

public sealed class CsvStatementReader(CsvStatementReaderOptions options) : IStatementReader
{
    private const char Separator = ',';
    private const char Quote = '"';
    private const string ShapeKeySeparator = "|";
    private const int FirstDataLineNumber = 2;

    private static readonly Dictionary<StatementColumn, string> RequiredColumnNames = new()
    {
        [StatementColumn.Date] = "date",
        [StatementColumn.Amount] = "amount",
        [StatementColumn.Account] = "account",
    };

    public async Task<StatementHeader> ReadHeaderAsync(Stream statement, CancellationToken cancellationToken)
    {
        using StreamReader reader = OpenReader(statement);
        string headerLine = await reader.ReadLineAsync(cancellationToken) ?? string.Empty;

        List<string> columnNames = [.. SplitLine(headerLine).Select(name => name.Trim())];
        List<string> normalisedNames = [.. columnNames.Select(name => name.ToLowerInvariant())];
        DropTrailingEmptyNames(normalisedNames);

        HashSet<StatementColumn> missingColumns =
        [
            .. RequiredColumnNames
                .Where(required => !normalisedNames.Contains(required.Value))
                .Select(required => required.Key),
        ];

        return new StatementHeader(columnNames, string.Join(ShapeKeySeparator, normalisedNames), missingColumns);
    }

    public async IAsyncEnumerable<StatementRow> ReadRowsAsync(
        Stream statement,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using StreamReader reader = OpenReader(statement);
        await reader.ReadLineAsync(cancellationToken);

        int lineNumber = FirstDataLineNumber;
        int rowsRead = 0;

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            // ReadLineAsync looks at the token only when it has to pull more bytes from the stream,
            // so a cancel that lands while the next lines are already buffered would go unnoticed.
            cancellationToken.ThrowIfCancellationRequested();
            rowsRead++;
            if (rowsRead > options.RowCeiling)
            {
                throw new InvalidDataException(
                    $"The file has more than {options.RowCeiling} rows, which is more than a statement can hold.");
            }

            yield return new StatementRow(lineNumber, SplitLine(line), line);
            lineNumber++;
        }
    }

    private static StreamReader OpenReader(Stream statement)
    {
        return new StreamReader(statement, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
    }

    private static List<string> SplitLine(string line)
    {
        List<string> values = [];
        StringBuilder value = new();
        bool insideQuotes = false;

        for (int index = 0; index < line.Length; index++)
        {
            char current = line[index];

            if (insideQuotes)
            {
                bool isEscapedQuote = current == Quote && index + 1 < line.Length && line[index + 1] == Quote;
                if (isEscapedQuote)
                {
                    value.Append(Quote);
                    index++;
                }
                else if (current == Quote)
                {
                    insideQuotes = false;
                }
                else
                {
                    value.Append(current);
                }
            }
            else if (current == Quote)
            {
                insideQuotes = true;
            }
            else if (current == Separator)
            {
                values.Add(value.ToString());
                value.Clear();
            }
            else
            {
                value.Append(current);
            }
        }

        values.Add(value.ToString());
        return values;
    }

    private static void DropTrailingEmptyNames(List<string> names)
    {
        while (names.Count > 0 && names[^1].Length == 0)
        {
            names.RemoveAt(names.Count - 1);
        }
    }
}
