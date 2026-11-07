using FinNavis.Application.Abstractions.Reads;
using FinNavis.Infrastructure.Import;
using FinNavis.Infrastructure.IntegrationTests.Fixtures;

namespace FinNavis.Infrastructure.IntegrationTests.Import;

public sealed class CsvStatementReaderExportTests
{
    private const char ByteOrderMark = '﻿';

    private static readonly string[] GroceriesRow =
        ["2023.01.02", "-19.5", "", "Cash wallet (UAH)", "Groceries", "Food", "Corner Bakery"];

    private static readonly string[] BalanceRow =
        ["2023.01.03", "4200", "", "Savings jar (UAH)", "Account balance", "", ""];

    private static readonly string[] TotalRowWithoutADate = ["", "-123456.78", "", "", "", "", ""];

    private static readonly string[] EmptyRowWithoutADate = ["", "", "", "", "", "", ""];

    public static TheoryData<string> LastNamesWithAStrayByteOrderMark => new()
    {
        "Note" + ByteOrderMark,
        ByteOrderMark + "Note",
    };

    public static TheoryData<string, StatementColumn> HeadersWithMissingColumns => new()
    {
        { "Date", StatementColumn.Date },
        { "Amount", StatementColumn.Amount },
        { "Account", StatementColumn.Account },
    };

    [Fact]
    public async Task ReadHeaderAsync_Export_ReadsTheFirstLineAsTheHeaderRow()
    {
        StatementHeader header = await ReadHeaderAsync(SyntheticFile.Export(SyntheticFile.ExportHeader, GroceriesRow));

        header.ColumnNames.Should().Equal(SyntheticFile.ExportHeader);
        header.ShapeKey.Should().Be(SyntheticFile.ExportShapeKey);
    }

    // The old tracker writes a byte order mark into its last column name. Left in, the key would
    // differ from the one the owner confirmed for an otherwise identical file.
    [Theory]
    [MemberData(nameof(LastNamesWithAStrayByteOrderMark))]
    public async Task ReadHeaderAsync_ExportWithAStrayByteOrderMarkInTheLastName_KeepsTheShapeKey(string lastName)
    {
        string[] headerRow = [.. SyntheticFile.ExportHeader[..^1], lastName];

        StatementHeader header = await ReadHeaderAsync(SyntheticFile.Export(headerRow, GroceriesRow));

        header.ShapeKey.Should().Be(SyntheticFile.ExportShapeKey);
    }

    [Fact]
    public async Task ReadHeaderAsync_ExportWithTheRealHeaderRow_NamesNoMissingColumn()
    {
        StatementHeader header = await ReadHeaderAsync(SyntheticFile.Export(SyntheticFile.ExportHeader, GroceriesRow));

        header.MissingColumns.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(HeadersWithMissingColumns))]
    public async Task ReadHeaderAsync_ExportWithoutARequiredColumn_NamesTheMissingOne(
        string removedColumn,
        StatementColumn expectedMissing)
    {
        string[] headerRow = [.. SyntheticFile.ExportHeader.Where(name => name != removedColumn)];

        StatementHeader header = await ReadHeaderAsync(SyntheticFile.Export(headerRow));

        header.MissingColumns.Should().BeEquivalentTo([expectedMissing]);
    }

    // An export writes one signed amount and no separate sign column. Its currency column is always
    // empty, but it is still where the column is.
    [Fact]
    public async Task ReadHeaderAsync_ExportWithTheRealHeaderRow_PointsAtTheDateTheAmountTheCurrencyAndTheAccount()
    {
        StatementHeader header = await ReadHeaderAsync(SyntheticFile.Export(SyntheticFile.ExportHeader, GroceriesRow));

        header.ColumnPositions.Should().BeEquivalentTo(new Dictionary<StatementColumn, int>
        {
            [StatementColumn.Date] = 0,
            [StatementColumn.Amount] = 1,
            [StatementColumn.Currency] = 2,
            [StatementColumn.Account] = 3,
        });
    }

    [Fact]
    public async Task ReadRowsAsync_Export_SplitsEachRowOnTabsAndNumbersItByItsLine()
    {
        List<StatementRow> rows = await ReadAllRowsAsync(
            SyntheticFile.Export(SyntheticFile.ExportHeader, GroceriesRow, BalanceRow));

        rows.Should().HaveCount(2);
        rows[0].Values.Should().Equal(GroceriesRow);
        rows[0].LineNumber.Should().Be(2);
        rows[1].LineNumber.Should().Be(3);
    }

    // The rows with no date at the end of the export are the tracker's own totals, not movements, so
    // they are neither recorded nor set aside. The balance rows are dated and are dropped later, by
    // the history migration, not by the reader.
    [Fact]
    public async Task ReadRowsAsync_ExportWithTrailingRowsWithoutADate_YieldsOnlyTheDatedRows()
    {
        List<StatementRow> rows = await ReadAllRowsAsync(SyntheticFile.Export(
            SyntheticFile.ExportHeader, GroceriesRow, BalanceRow, TotalRowWithoutADate, EmptyRowWithoutADate));

        rows.Select(row => row.Values[0]).Should().Equal("2023.01.02", "2023.01.03");
        rows[1].Values[4].Should().Be("Account balance");
    }

    private static async Task<StatementHeader> ReadHeaderAsync(byte[] file)
    {
        CsvStatementReader reader = new(CsvStatementReaderOptions.WithoutCeiling);
        using MemoryStream export = new(file);

        return await reader.ReadHeaderAsync(export, ImportFileKind.Export, CancellationToken.None);
    }

    private static async Task<List<StatementRow>> ReadAllRowsAsync(byte[] file)
    {
        CsvStatementReader reader = new(CsvStatementReaderOptions.WithoutCeiling);
        using MemoryStream export = new(file);
        List<StatementRow> rows = [];

        await foreach (StatementRow row in reader.ReadRowsAsync(export, ImportFileKind.Export, CancellationToken.None))
        {
            rows.Add(row);
        }

        return rows;
    }
}
