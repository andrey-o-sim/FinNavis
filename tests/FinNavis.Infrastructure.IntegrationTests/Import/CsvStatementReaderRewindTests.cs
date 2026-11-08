using FinNavis.Application.Abstractions.Reads;
using FinNavis.Infrastructure.Import;
using FinNavis.Infrastructure.IntegrationTests.Fakes;
using FinNavis.Infrastructure.IntegrationTests.Fixtures;

namespace FinNavis.Infrastructure.IntegrationTests.Import;

public sealed class CsvStatementReaderRewindTests
{
    private const int SampleStatementRowCount = 38;

    private readonly CsvStatementReader _reader = new(CsvStatementReaderOptions.WithoutCeiling);

    [Fact]
    public async Task ReadRowsAsync_AfterReadHeaderAsyncOnTheSameStream_YieldsEveryRowFromTheFirst()
    {
        await using FileStream statement = File.OpenRead(SampleFiles.StatementPath);

        StatementHeader header = await _reader.ReadHeaderAsync(statement, ImportFileKind.Statement, CancellationToken.None);
        List<StatementRow> rows = await ReadAllRowsAsync(statement);

        header.ShapeKey.Should().Be(SyntheticFile.StatementShapeKey);
        rows.Should().HaveCount(SampleStatementRowCount);
        rows[0].LineNumber.Should().Be(3);
        rows[0].Values[1].Should().Be("Продукти");
    }

    [Fact]
    public async Task ReadRowsAsync_TwiceOnTheSameStream_YieldsTheSameRowsBothTimes()
    {
        await using FileStream statement = File.OpenRead(SampleFiles.StatementPath);

        List<StatementRow> first = await ReadAllRowsAsync(statement);
        List<StatementRow> second = await ReadAllRowsAsync(statement);

        second.Should().BeEquivalentTo(first, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task ReadHeaderAsync_StreamLeftAtItsEnd_ReadsTheHeaderFromTheFirstLine()
    {
        await using FileStream statement = File.OpenRead(SampleFiles.StatementPath);
        statement.Seek(0, SeekOrigin.End);

        StatementHeader header = await _reader.ReadHeaderAsync(statement, ImportFileKind.Statement, CancellationToken.None);

        header.ShapeKey.Should().Be(SyntheticFile.StatementShapeKey);
    }

    // An upload that cannot seek is read from where it stands, as before: the reader never tries to
    // move it, so a stream that refuses to seek does not fail the read.
    [Fact]
    public async Task ReadRowsAsync_StreamThatCannotSeek_ReadsItFromWhereItStands()
    {
        await using ReadCountingStream statement = new(File.OpenRead(SampleFiles.StatementPath));

        List<StatementRow> rows = await ReadAllRowsAsync(statement);

        rows.Should().HaveCount(SampleStatementRowCount);
    }

    private async Task<List<StatementRow>> ReadAllRowsAsync(Stream statement)
    {
        List<StatementRow> rows = [];

        await foreach (StatementRow row in _reader.ReadRowsAsync(statement, ImportFileKind.Statement, CancellationToken.None))
        {
            rows.Add(row);
        }

        return rows;
    }
}
