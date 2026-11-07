using System.Text;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Infrastructure.Import;
using FinNavis.Infrastructure.IntegrationTests.Fakes;
using FinNavis.Infrastructure.IntegrationTests.Fixtures;

namespace FinNavis.Infrastructure.IntegrationTests.Import;

public sealed class CsvStatementReaderRowsTests
{
    [Fact]
    public async Task ReadRowsAsync_File_SkipsTheHeaderAndYieldsEachRowAsTheFileWroteIt()
    {
        const string file = "Date,Description,Category,Amount\n"
            + "05.03.2026,Corner Bakery,Groceries,-45.50\n"
            + "06.03.2026,\"Pixel Books, Ltd\",Books,-120.00\n";

        List<StatementRow> rows = await ReadAllRowsAsync(Encoding.UTF8.GetBytes(file));

        rows.Should().HaveCount(2);
        rows[0].Values.Should().Equal("05.03.2026", "Corner Bakery", "Groceries", "-45.50");
        rows[0].RawLine.Should().Be("05.03.2026,Corner Bakery,Groceries,-45.50");
        rows[1].Values.Should().Equal("06.03.2026", "Pixel Books, Ltd", "Books", "-120.00");
        rows[1].RawLine.Should().Be("06.03.2026,\"Pixel Books, Ltd\",Books,-120.00");
    }

    [Fact]
    public async Task ReadRowsAsync_File_NumbersEachRowByItsLineInTheFile()
    {
        const string file = "Date,Amount\n01.03.2026,-1.00\n02.03.2026,-2.00\n03.03.2026,-3.00\n";

        List<StatementRow> rows = await ReadAllRowsAsync(Encoding.UTF8.GetBytes(file));

        rows.Select(row => row.LineNumber).Should().Equal(2, 3, 4);
    }

    [Fact]
    public async Task ReadRowsAsync_ExportOf1500Rows_YieldsEveryRow()
    {
        List<StatementRow> rows = await ReadAllRowsAsync(await File.ReadAllBytesAsync(SampleFiles.ExportPath));

        rows.Should().HaveCount(SampleFiles.ExportRowCount);
    }

    // A file with millions of rows must be refusable before it exhausts memory, which is only
    // possible when the first row arrives before the rest of the file has been read.
    [Fact]
    public async Task ReadRowsAsync_ExportOf1500Rows_YieldsTheFirstRowBeforeReadingTheWholeFile()
    {
        CsvStatementReader reader = new(CsvStatementReaderOptions.WithoutCeiling);
        await using ReadCountingStream statement = new(File.OpenRead(SampleFiles.ExportPath));

        await using IAsyncEnumerator<StatementRow> rows = reader
            .ReadRowsAsync(statement, CancellationToken.None)
            .GetAsyncEnumerator();

        (await rows.MoveNextAsync()).Should().BeTrue();
        statement.BytesRead.Should().BeLessThan(statement.Length / 4);
    }

    [Fact]
    public async Task ReadRowsAsync_CancelledPartWay_StopsWithoutReadingTheRest()
    {
        const int rowsBeforeCancel = 10;
        CsvStatementReader reader = new(CsvStatementReaderOptions.WithoutCeiling);
        await using ReadCountingStream statement = new(File.OpenRead(SampleFiles.ExportPath));
        using CancellationTokenSource cancellation = new();
        int rowsRead = 0;

        Func<Task> readAll = async () =>
        {
            await foreach (StatementRow _ in reader.ReadRowsAsync(statement, cancellation.Token))
            {
                rowsRead++;
                if (rowsRead == rowsBeforeCancel)
                {
                    await cancellation.CancelAsync();
                }
            }
        };

        await readAll.Should().ThrowAsync<OperationCanceledException>();
        rowsRead.Should().Be(rowsBeforeCancel);
        statement.BytesRead.Should().BeLessThan(statement.Length / 4);
    }

    // The ceiling's value is still an open question for the owner, so the reader reads every row
    // until one is configured. When it is, a file past it is refused while it streams, before the
    // pipeline has recorded anything.
    [Fact]
    public async Task ReadRowsAsync_FileLongerThanTheRowCeiling_RefusesItBeforeReadingTheRest()
    {
        const int ceiling = 100;
        CsvStatementReader reader = new(new CsvStatementReaderOptions(RowCeiling: ceiling));
        await using ReadCountingStream statement = new(File.OpenRead(SampleFiles.ExportPath));
        int rowsRead = 0;

        Func<Task> readAll = async () =>
        {
            await foreach (StatementRow _ in reader.ReadRowsAsync(statement, CancellationToken.None))
            {
                rowsRead++;
            }
        };

        await readAll.Should().ThrowAsync<InvalidDataException>();
        rowsRead.Should().Be(ceiling);
        statement.BytesRead.Should().BeLessThan(statement.Length / 2);
    }

    [Fact]
    public async Task ReadRowsAsync_FileExactlyAtTheRowCeiling_YieldsEveryRow()
    {
        CsvStatementReader reader = new(new CsvStatementReaderOptions(RowCeiling: SampleFiles.ExportRowCount));
        await using FileStream statement = File.OpenRead(SampleFiles.ExportPath);
        int rowsRead = 0;

        await foreach (StatementRow _ in reader.ReadRowsAsync(statement, CancellationToken.None))
        {
            rowsRead++;
        }

        rowsRead.Should().Be(SampleFiles.ExportRowCount);
    }

    private static async Task<List<StatementRow>> ReadAllRowsAsync(byte[] file)
    {
        CsvStatementReader reader = new(CsvStatementReaderOptions.WithoutCeiling);
        using MemoryStream statement = new(file);
        List<StatementRow> rows = [];

        await foreach (StatementRow row in reader.ReadRowsAsync(statement, CancellationToken.None))
        {
            rows.Add(row);
        }

        return rows;
    }
}
