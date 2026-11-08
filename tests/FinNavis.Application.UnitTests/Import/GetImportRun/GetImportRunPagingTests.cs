using System.Text;
using FinNavis.Application.Import.GetImportRun;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;
using GetImportRunUseCase = FinNavis.Application.Import.GetImportRun.GetImportRun;

namespace FinNavis.Application.UnitTests.Import.GetImportRun;

public sealed class GetImportRunPagingTests
{
    private const string NotMintedMessage = "That is not a cursor this server handed out.";

    private static readonly DateTimeOffset LoadedAt = new(2026, 11, 7, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeImportRunRepository _runs = new();

    [Fact]
    public void FirstPage_Limit_IsTheDefaultOfFifty()
    {
        SetAsidePageRequest.FirstPage.Should().Be(new SetAsidePageRequest(null, null, 50));
        SetAsidePageRequest.DefaultLimit.Should().Be(50);
        SetAsidePageRequest.MinLimit.Should().Be(1);
        SetAsidePageRequest.MaxLimit.Should().Be(200);
    }

    [Fact]
    public async Task ExecuteAsync_FirstPageOfMoreRowsThanTheDefault_ReturnsFiftyRowsAndOnlyANextCursor()
    {
        ImportRun run = RunWithSetAsideRows(60);

        SetAsideRowPage page = await ReadPageAsync(run.Id, SetAsidePageRequest.FirstPage);

        page.Items.Select(row => row.RowNumber).Should().Equal(Enumerable.Range(1, 50));
        page.HasNext.Should().BeTrue();
        page.HasPrev.Should().BeFalse();
        page.NextCursor.Should().NotBeNullOrEmpty();
        page.PrevCursor.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_FewerRowsThanTheLimit_ReturnsThemAllWithNoCursor()
    {
        ImportRun run = RunWithSetAsideRows(3);

        SetAsideRowPage page = await ReadPageAsync(run.Id, new SetAsidePageRequest(null, null, 3));

        page.Items.Select(row => row.RowNumber).Should().Equal(1, 2, 3);
        page.HasNext.Should().BeFalse();
        page.HasPrev.Should().BeFalse();
        page.NextCursor.Should().BeNull();
        page.PrevCursor.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_AfterTheNextCursor_ReturnsTheFollowingRowsAndBothCursors()
    {
        ImportRun run = RunWithSetAsideRows(7);
        SetAsideRowPage first = await ReadPageAsync(run.Id, new SetAsidePageRequest(null, null, 3));

        SetAsideRowPage second = await ReadPageAsync(run.Id, new SetAsidePageRequest(first.NextCursor, null, 3));

        second.Items.Select(row => row.RowNumber).Should().Equal(4, 5, 6);
        second.HasNext.Should().BeTrue();
        second.HasPrev.Should().BeTrue();
        second.NextCursor.Should().NotBeNull();
        second.PrevCursor.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_LastPage_HasNoNextCursor()
    {
        ImportRun run = RunWithSetAsideRows(5);
        SetAsideRowPage first = await ReadPageAsync(run.Id, new SetAsidePageRequest(null, null, 3));

        SetAsideRowPage last = await ReadPageAsync(run.Id, new SetAsidePageRequest(first.NextCursor, null, 3));

        last.Items.Select(row => row.RowNumber).Should().Equal(4, 5);
        last.HasNext.Should().BeFalse();
        last.NextCursor.Should().BeNull();
        last.HasPrev.Should().BeTrue();
        last.PrevCursor.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_BeforeThePrevCursor_ReturnsThePreviousPage()
    {
        ImportRun run = RunWithSetAsideRows(7);
        SetAsideRowPage first = await ReadPageAsync(run.Id, new SetAsidePageRequest(null, null, 3));
        SetAsideRowPage second = await ReadPageAsync(run.Id, new SetAsidePageRequest(first.NextCursor, null, 3));

        SetAsideRowPage back = await ReadPageAsync(run.Id, new SetAsidePageRequest(null, second.PrevCursor, 3));

        back.Items.Select(row => row.RowNumber).Should().Equal(1, 2, 3);
        back.HasPrev.Should().BeFalse();
        back.PrevCursor.Should().BeNull();
        back.HasNext.Should().BeTrue();
        back.NextCursor.Should().Be(first.NextCursor);
    }

    // Row numbers in a run have gaps — only the rows that were set aside are stored — so a page
    // boundary is the row after the cursor, never the cursor plus one.
    [Fact]
    public async Task ExecuteAsync_WalkingEveryPageOfRowsWithGaps_VisitsEachRowOnceInFileOrder()
    {
        int[] rowNumbers = [12, 3, 40, 8, 21, 5, 33];
        ImportRun run = Run();
        foreach (int rowNumber in rowNumbers)
        {
            run.SetAside(Guid.CreateVersion7(), rowNumber, SetAsideReason.NoRuleCovers, "TEST", $"line {rowNumber}");
        }

        _runs.Seed(run);
        List<int> visited = [];
        string? after = null;

        do
        {
            SetAsideRowPage page = await ReadPageAsync(run.Id, new SetAsidePageRequest(after, null, 2));
            visited.AddRange(page.Items.Select(row => row.RowNumber));
            after = page.NextCursor;
        }
        while (after is not null);

        visited.Should().Equal(3, 5, 8, 12, 21, 33, 40);
    }

    [Fact]
    public async Task ExecuteAsync_MintedCursor_DoesNotSpellTheRowNumberInPlainText()
    {
        ImportRun run = RunWithSetAsideRows(3);

        SetAsideRowPage page = await ReadPageAsync(run.Id, new SetAsidePageRequest(null, null, 1));

        page.NextCursor.Should().NotBe("1");
        int.TryParse(page.NextCursor, out _).Should().BeFalse();
        page.NextCursor!.Length.Should().BeLessThanOrEqualTo(200);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(200)]
    public async Task ExecuteAsync_LimitOnTheBound_IsAccepted(int limit)
    {
        ImportRun run = RunWithSetAsideRows(3);

        SetAsideRowPage page = await ReadPageAsync(run.Id, new SetAsidePageRequest(null, null, limit));

        page.Items.Should().HaveCount(Math.Min(limit, 3));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(201)]
    [InlineData(5000)]
    public async Task ExecuteAsync_LimitOutsideOneToTwoHundred_IsRefusedAsInvalidPaging(int limit)
    {
        ImportRun run = RunWithSetAsideRows(3);

        DomainRuleViolationException refusal =
            await RefusalAsync(run.Id, new SetAsidePageRequest(null, null, limit));

        refusal.Code.Should().Be("Import.InvalidPaging");
        refusal.Message.Should().Be("'limit' must be between 1 and 200.");
        refusal.Details.Should().ContainKey("limit").WhoseValue.Should().Be(limit);
    }

    [Theory]
    [InlineData("not-a-cursor")]
    [InlineData("1")]
    [InlineData("!!!")]
    [InlineData("")]
    public async Task ExecuteAsync_MalformedAfterCursor_IsRefusedAsInvalidPaging(string cursor)
    {
        ImportRun run = RunWithSetAsideRows(3);

        DomainRuleViolationException refusal =
            await RefusalAsync(run.Id, new SetAsidePageRequest(cursor, null, 2));

        refusal.Code.Should().Be("Import.InvalidPaging");
        refusal.Message.Should().Be(NotMintedMessage);
    }

    [Fact]
    public async Task ExecuteAsync_MalformedBeforeCursor_IsRefusedAsInvalidPaging()
    {
        ImportRun run = RunWithSetAsideRows(3);

        DomainRuleViolationException refusal =
            await RefusalAsync(run.Id, new SetAsidePageRequest(null, "not-a-cursor", 2));

        refusal.Code.Should().Be("Import.InvalidPaging");
    }

    [Fact]
    public async Task ExecuteAsync_CursorLongerThanTwoHundredCharacters_IsRefusedAsInvalidPaging()
    {
        ImportRun run = RunWithSetAsideRows(3);
        string cursor = new('A', 201);

        DomainRuleViolationException refusal =
            await RefusalAsync(run.Id, new SetAsidePageRequest(cursor, null, 2));

        refusal.Code.Should().Be("Import.InvalidPaging");
    }

    // A client that builds a cursor by hand — here a guess at the encoding — is refused, so the
    // token stays the server's to shape.
    [Fact]
    public async Task ExecuteAsync_HandBuiltCursor_IsRefusedAsInvalidPaging()
    {
        ImportRun run = RunWithSetAsideRows(3);
        string handBuilt = Convert.ToBase64String(Encoding.UTF8.GetBytes("row_number=1"));

        DomainRuleViolationException refusal =
            await RefusalAsync(run.Id, new SetAsidePageRequest(handBuilt, null, 2));

        refusal.Code.Should().Be("Import.InvalidPaging");
    }

    [Fact]
    public async Task ExecuteAsync_CursorMintedForAnotherRun_IsRefusedAsInvalidPaging()
    {
        ImportRun other = RunWithSetAsideRows(3);
        ImportRun run = RunWithSetAsideRows(3);
        SetAsideRowPage otherPage = await ReadPageAsync(other.Id, new SetAsidePageRequest(null, null, 1));

        DomainRuleViolationException refusal =
            await RefusalAsync(run.Id, new SetAsidePageRequest(otherPage.NextCursor, null, 1));

        refusal.Code.Should().Be("Import.InvalidPaging");
        refusal.Message.Should().Be(NotMintedMessage);
    }

    [Fact]
    public async Task ExecuteAsync_BothCursorsGiven_IsRefusedAsInvalidPaging()
    {
        ImportRun run = RunWithSetAsideRows(7);
        SetAsideRowPage first = await ReadPageAsync(run.Id, new SetAsidePageRequest(null, null, 3));
        SetAsideRowPage second = await ReadPageAsync(run.Id, new SetAsidePageRequest(first.NextCursor, null, 3));

        DomainRuleViolationException refusal =
            await RefusalAsync(run.Id, new SetAsidePageRequest(second.NextCursor, second.PrevCursor, 3));

        refusal.Code.Should().Be("Import.InvalidPaging");
    }

    private ImportRun RunWithSetAsideRows(int count)
    {
        ImportRun run = Run();
        for (int rowNumber = count; rowNumber >= 1; rowNumber--)
        {
            run.SetAside(Guid.CreateVersion7(), rowNumber, SetAsideReason.NoRuleCovers, "TEST", $"line {rowNumber}");
        }

        _runs.Seed(run);

        return run;
    }

    private static ImportRun Run() =>
        ImportRun.Record(Guid.CreateVersion7(), "test-history.csv", ImportRunKind.HistoryMigration, accountId: null, LoadedAt);

    private async Task<SetAsideRowPage> ReadPageAsync(Guid importRunId, SetAsidePageRequest page)
    {
        ImportRunReport report =
            await new GetImportRunUseCase(_runs).ExecuteAsync(importRunId, page, CancellationToken.None);

        return report.SetAside;
    }

    private async Task<DomainRuleViolationException> RefusalAsync(Guid importRunId, SetAsidePageRequest page)
    {
        Func<Task> read = () => ReadPageAsync(importRunId, page);

        return (await read.Should().ThrowAsync<DomainRuleViolationException>()).Which;
    }
}
