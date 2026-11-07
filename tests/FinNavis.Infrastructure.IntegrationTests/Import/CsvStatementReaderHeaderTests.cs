using System.Text;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Infrastructure.Import;

namespace FinNavis.Infrastructure.IntegrationTests.Import;

public sealed class CsvStatementReaderHeaderTests
{
    public static TheoryData<string, string> HeaderRows => new()
    {
        { "Date,Description,Category,Amount\n", "date|description|category|amount" },
        { "  Date , Description ,Amount\n", "date|description|amount" },
        { "DATE,Amount,Category\n", "date|amount|category" },
        { "Amount,Date,Category\n", "amount|date|category" },
        { "Date,Amount,,\n", "date|amount" },
        { "Date,Amount, ,  \n", "date|amount" },
        { "Date,,Amount\n", "date||amount" },
        { "Date,Amount\r\n01.03.2026,-12.50\r\n", "date|amount" },
        { "\"Date\",\"Amount, UAH\",Category\n", "date|amount, uah|category" },
    };

    public static TheoryData<string, StatementColumn[]> HeadersWithMissingColumns => new()
    {
        { "Description,Category,Amount,Account\n", [StatementColumn.Date] },
        { "Date,Description,Category,Account\n", [StatementColumn.Amount] },
        { "Date,Description,Category,Amount\n", [StatementColumn.Account] },
        { "Description,Category\n", [StatementColumn.Date, StatementColumn.Amount, StatementColumn.Account] },
    };

    // The shape key is the identity of a mapping profile. Two files of one bank that differ only in
    // case, padding or a trailing separator must land on the same profile, or the owner is asked to
    // confirm the same rules again every month.
    [Theory]
    [MemberData(nameof(HeaderRows))]
    public async Task ReadHeaderAsync_HeaderRow_BuildsTheNormalisedShapeKey(string file, string expectedShapeKey)
    {
        StatementHeader header = await ReadHeaderAsync(file);

        header.ShapeKey.Should().Be(expectedShapeKey);
    }

    // Spreadsheet programs often save a CSV with a byte order mark in front of the first column
    // name. Left in, it would make the same bank's file a new shape.
    [Fact]
    public async Task ReadHeaderAsync_HeaderAfterByteOrderMark_LeavesTheMarkOutOfTheShapeKey()
    {
        byte[] file = [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes("Date,Amount\n")];

        StatementHeader header = await ReadHeaderAsync(file);

        header.ShapeKey.Should().Be("date|amount");
    }

    [Fact]
    public async Task ReadHeaderAsync_HeaderRow_ReturnsTheColumnNamesInFileOrder()
    {
        StatementHeader header = await ReadHeaderAsync("Date,Description,Category,Amount\n");

        header.ColumnNames.Should().Equal("Date", "Description", "Category", "Amount");
    }

    [Theory]
    [MemberData(nameof(HeadersWithMissingColumns))]
    public async Task ReadHeaderAsync_HeaderWithoutARequiredColumn_NamesTheMissingOne(
        string file,
        StatementColumn[] expectedMissing)
    {
        StatementHeader header = await ReadHeaderAsync(file);

        header.MissingColumns.Should().BeEquivalentTo(expectedMissing);
    }

    [Fact]
    public async Task ReadHeaderAsync_HeaderNamingDateAmountAndAccount_NamesNoMissingColumn()
    {
        StatementHeader header = await ReadHeaderAsync(" DATE ,Category,amount,Account\n");

        header.MissingColumns.Should().BeEmpty();
    }

    private static Task<StatementHeader> ReadHeaderAsync(string file)
    {
        return ReadHeaderAsync(Encoding.UTF8.GetBytes(file));
    }

    private static async Task<StatementHeader> ReadHeaderAsync(byte[] file)
    {
        CsvStatementReader reader = new(CsvStatementReaderOptions.WithoutCeiling);
        using MemoryStream statement = new(file);

        return await reader.ReadHeaderAsync(statement, CancellationToken.None);
    }
}
