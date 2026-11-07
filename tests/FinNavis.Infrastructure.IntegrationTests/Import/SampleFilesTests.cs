using System.Text;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Infrastructure.Import;
using FinNavis.Infrastructure.IntegrationTests.Fixtures;

namespace FinNavis.Infrastructure.IntegrationTests.Import;

public sealed class SampleFilesTests
{
    private const string CrLf = "\r\n";
    private const char ByteOrderMark = '﻿';

    // A statement covers one account, which the owner names when handing it over, so the file
    // itself names none.
    [Fact]
    public async Task ReadHeaderAsync_SampleStatement_FindsTheDateAndTheAmountUnderTheRealNames()
    {
        StatementHeader header = await ReadHeaderAsync(SampleFiles.StatementPath, ImportFileKind.Statement);

        header.ShapeKey.Should().Be(SyntheticFile.StatementShapeKey);
        header.MissingColumns.Should().BeEquivalentTo([StatementColumn.Account]);
    }

    [Fact]
    public async Task ReadHeaderAsync_SampleExport_FindsTheDateTheAmountAndTheAccountUnderTheRealNames()
    {
        StatementHeader header = await ReadHeaderAsync(SampleFiles.ExportPath, ImportFileKind.Export);

        header.ShapeKey.Should().Be(SyntheticFile.ExportShapeKey);
        header.MissingColumns.Should().BeEmpty();
    }

    [Fact]
    public void SampleStatement_CommittedFile_IsNamedTestStatement()
    {
        Path.GetFileName(SampleFiles.StatementPath).Should().Be("test-statement.csv");
        File.Exists(SampleFiles.StatementPath).Should().BeTrue();
    }

    // A sample that drifts from the real shape lets the whole suite pass against a file no bank
    // produces, so the shape itself is pinned here, byte by byte where it matters.
    [Fact]
    public async Task SampleStatement_CommittedFile_CopiesTheRealShape()
    {
        byte[] bytes = await File.ReadAllBytesAsync(SampleFiles.StatementPath);
        string text = Encoding.UTF8.GetString(bytes);
        string[] lines = text.Split(CrLf);

        bytes.Should().StartWith(Encoding.UTF8.GetPreamble());
        text.Replace(CrLf, string.Empty).Should().NotContain("\n");
        lines[0].Should().EndWith(SyntheticFile.StatementTitlePadding);
        lines[0].Count(character => character == ',').Should().Be(SyntheticFile.StatementTitlePadding.Length);
        lines[1].Should().Be(string.Join(',', SyntheticFile.StatementHeader));
        lines.Should().Contain(line => line.Contains("\",") && line.Contains(", "));
    }

    [Fact]
    public async Task SampleExport_CommittedFile_CopiesTheRealShape()
    {
        byte[] bytes = await File.ReadAllBytesAsync(SampleFiles.ExportPath);
        string text = Encoding.Unicode.GetString(bytes);
        string[] lines = text.TrimEnd('\r', '\n').Split(CrLf);

        bytes.Should().StartWith(Encoding.Unicode.GetPreamble());
        text.Replace(CrLf, string.Empty).Should().NotContain("\n");
        lines[0].TrimStart(ByteOrderMark).Split('\t')[..^1].Should().Equal(SyntheticFile.ExportHeader[..^1]);
        lines[0].Split('\t')[^1].Should().Contain(ByteOrderMark.ToString());
        lines.Skip(1).Should().AllSatisfy(line => line.Split('\t')[2].Should().BeEmpty());
        lines.Should().Contain(line => line.Split('\t')[4] == "Account balance");
        lines[^1].Split('\t')[0].Should().BeEmpty();
        lines.Skip(1).Count(line => line.Split('\t')[0].Length > 0).Should().Be(SampleFiles.ExportRowCount);
    }

    private static async Task<StatementHeader> ReadHeaderAsync(string path, ImportFileKind kind)
    {
        CsvStatementReader reader = new(CsvStatementReaderOptions.WithoutCeiling);
        await using FileStream file = File.OpenRead(path);

        return await reader.ReadHeaderAsync(file, kind, CancellationToken.None);
    }
}
