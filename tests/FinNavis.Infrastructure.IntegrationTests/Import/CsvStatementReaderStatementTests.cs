using FinNavis.Application.Abstractions.Reads;
using FinNavis.Infrastructure.Import;
using FinNavis.Infrastructure.IntegrationTests.Fixtures;

namespace FinNavis.Infrastructure.IntegrationTests.Import;

public sealed class CsvStatementReaderStatementTests
{
    private const string MarchTitle = "Виписка за період 01.03.2026 - 31.03.2026";
    private const string AprilTitle = "Виписка за період 01.04.2026 - 30.04.2026";

    private const string BakeryRow =
        "05.03.2026 14:22:10,Продукти,**** 0001,Corner Bakery,-45.5,UAH,45.5,UAH,24954.5,UAH";

    private const string BookshopRow =
        "06.03.2026 09:01:44,Книги,**** 0002,\"Pixel Books, Ltd\",-120,UAH,120,UAH,24834.5,UAH";

    // The bank's spreadsheet pads the title line with commas to the width of the header, so the
    // line looks like a row with one value. Whatever it says, it is never the header row.
    public static TheoryData<string> TitleLines => new()
    {
        SyntheticFile.StatementTitle(MarchTitle),
        SyntheticFile.StatementTitle("Дата"),
        SyntheticFile.StatementTitle("\"Statement, March 2026\""),
        SyntheticFile.StatementTitle(string.Empty),
    };

    public static TheoryData<string, StatementColumn[]> HeadersWithMissingColumns => new()
    {
        { "Дата", [StatementColumn.Date, StatementColumn.Account] },
        { "Сума в валюті транзакції", [StatementColumn.Amount, StatementColumn.Account] },
    };

    [Theory]
    [MemberData(nameof(TitleLines))]
    public async Task ReadHeaderAsync_StatementWithAnyTitleLine_ReadsTheSecondLineAsTheHeaderRow(string titleLine)
    {
        StatementHeader header = await ReadHeaderAsync(
            SyntheticFile.Statement(titleLine, SyntheticFile.StatementHeader, BakeryRow));

        header.ShapeKey.Should().Be(SyntheticFile.StatementShapeKey);
        header.ColumnNames.Should().Equal(SyntheticFile.StatementHeader);
    }

    // The title names the period, so it changes every month. Were it part of the key, the owner
    // would be asked to confirm the same bank's rules again for every statement.
    [Fact]
    public async Task ReadHeaderAsync_TwoMonthsWithDifferentTitleLines_GiveTheSameShapeKey()
    {
        StatementHeader march = await ReadHeaderAsync(SyntheticFile.Statement(
            SyntheticFile.StatementTitle(MarchTitle), SyntheticFile.StatementHeader, BakeryRow));
        StatementHeader april = await ReadHeaderAsync(SyntheticFile.Statement(
            SyntheticFile.StatementTitle(AprilTitle), SyntheticFile.StatementHeader, BakeryRow));

        april.ShapeKey.Should().Be(march.ShapeKey);
    }

    // A statement covers the one account the owner names, so its card column is not an account
    // column and the account is the only one the reader reports missing.
    [Fact]
    public async Task ReadHeaderAsync_StatementWithTheRealHeaderRow_NamesOnlyTheAccountAsMissing()
    {
        StatementHeader header = await ReadHeaderAsync(SyntheticFile.Statement(
            SyntheticFile.StatementTitle(MarchTitle), SyntheticFile.StatementHeader, BakeryRow));

        header.MissingColumns.Should().BeEquivalentTo([StatementColumn.Account]);
    }

    [Theory]
    [MemberData(nameof(HeadersWithMissingColumns))]
    public async Task ReadHeaderAsync_StatementWithoutARequiredColumn_NamesTheMissingOne(
        string removedColumn,
        StatementColumn[] expectedMissing)
    {
        string[] headerRow = [.. SyntheticFile.StatementHeader.Where(name => name != removedColumn)];

        StatementHeader header = await ReadHeaderAsync(
            SyntheticFile.Statement(SyntheticFile.StatementTitle(MarchTitle), headerRow));

        header.MissingColumns.Should().BeEquivalentTo(expectedMissing);
    }

    // The amount recorded is the purchase amount, which the bank writes unsigned; its sign comes from
    // the card amount, and the purchase currency is the one checked against the ledger's currency.
    [Fact]
    public async Task ReadHeaderAsync_StatementWithTheRealHeaderRow_PointsAtTheCategoryThePurchaseAmountItsSignAndItsCurrency()
    {
        StatementHeader header = await ReadHeaderAsync(SyntheticFile.Statement(
            SyntheticFile.StatementTitle(MarchTitle), SyntheticFile.StatementHeader, BakeryRow));

        header.ColumnPositions.Should().BeEquivalentTo(new Dictionary<StatementColumn, int>
        {
            [StatementColumn.Date] = 0,
            [StatementColumn.Category] = 1,
            [StatementColumn.AmountSign] = 4,
            [StatementColumn.Amount] = 6,
            [StatementColumn.Currency] = 7,
        });
    }

    [Fact]
    public async Task ReadRowsAsync_Statement_SkipsTheTitleLineAndTheHeaderRow()
    {
        List<StatementRow> rows = await ReadAllRowsAsync(SyntheticFile.Statement(
            SyntheticFile.StatementTitle(MarchTitle), SyntheticFile.StatementHeader, BakeryRow, BookshopRow));

        rows.Should().HaveCount(2);
        rows[0].Values.Should().Equal(
            "05.03.2026 14:22:10", "Продукти", "**** 0001", "Corner Bakery", "-45.5", "UAH", "45.5", "UAH",
            "24954.5", "UAH");
        rows[0].RawLine.Should().Be(BakeryRow);
    }

    [Fact]
    public async Task ReadRowsAsync_Statement_NumbersEachRowByItsLineInTheFile()
    {
        List<StatementRow> rows = await ReadAllRowsAsync(SyntheticFile.Statement(
            SyntheticFile.StatementTitle(MarchTitle), SyntheticFile.StatementHeader, BakeryRow, BookshopRow));

        rows.Select(row => row.LineNumber).Should().Equal(3, 4);
    }

    [Fact]
    public async Task ReadRowsAsync_StatementRowWithAQuotedComma_ReadsTheFieldAsOneValue()
    {
        List<StatementRow> rows = await ReadAllRowsAsync(SyntheticFile.Statement(
            SyntheticFile.StatementTitle(MarchTitle), SyntheticFile.StatementHeader, BookshopRow));

        rows.Should().ContainSingle();
        rows[0].Values.Should().HaveCount(SyntheticFile.StatementHeader.Length);
        rows[0].Values[3].Should().Be("Pixel Books, Ltd");
    }

    private static async Task<StatementHeader> ReadHeaderAsync(byte[] file)
    {
        CsvStatementReader reader = new(CsvStatementReaderOptions.WithoutCeiling);
        using MemoryStream statement = new(file);

        return await reader.ReadHeaderAsync(statement, ImportFileKind.Statement, CancellationToken.None);
    }

    private static async Task<List<StatementRow>> ReadAllRowsAsync(byte[] file)
    {
        CsvStatementReader reader = new(CsvStatementReaderOptions.WithoutCeiling);
        using MemoryStream statement = new(file);
        List<StatementRow> rows = [];

        await foreach (StatementRow row in reader.ReadRowsAsync(statement, ImportFileKind.Statement, CancellationToken.None))
        {
            rows.Add(row);
        }

        return rows;
    }
}
