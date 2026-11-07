using FinNavis.Application.Abstractions.Reads;
using FinNavis.Infrastructure.Import;
using FinNavis.Infrastructure.IntegrationTests.Fixtures;

namespace FinNavis.Infrastructure.IntegrationTests.Import;

public sealed class SampleFilesTests
{
    // A statement covers one account, which the owner names when handing it over, so the file
    // itself names none.
    [Fact]
    public async Task ReadHeaderAsync_SampleStatement_FindsTheDateAndTheAmount()
    {
        StatementHeader header = await ReadHeaderAsync(SampleFiles.StatementPath);

        header.MissingColumns.Should().BeEquivalentTo([StatementColumn.Account]);
    }

    [Fact]
    public async Task ReadHeaderAsync_SampleExport_FindsTheDateTheAmountAndTheAccount()
    {
        StatementHeader header = await ReadHeaderAsync(SampleFiles.ExportPath);

        header.MissingColumns.Should().BeEmpty();
    }

    [Fact]
    public void SampleStatement_CommittedFile_IsNamedTestStatement()
    {
        Path.GetFileName(SampleFiles.StatementPath).Should().Be("test-statement.csv");
        File.Exists(SampleFiles.StatementPath).Should().BeTrue();
    }

    private static async Task<StatementHeader> ReadHeaderAsync(string path)
    {
        CsvStatementReader reader = new(CsvStatementReaderOptions.WithoutCeiling);
        await using FileStream statement = File.OpenRead(path);

        return await reader.ReadHeaderAsync(statement, CancellationToken.None);
    }
}
