using FinNavis.Application.Abstractions.Reads;
using FinNavis.Application.Reporting;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.UnitTests.Reporting;

public sealed class GetHistoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly From = new(2026, 10, 1);
    private static readonly DateOnly To = new(2026, 10, 31);

    private readonly FakeLedgerQueries _ledger = new();

    // Each row is marked as a transaction or a transfer by being one record type or the other,
    // so a transaction can never carry a destination account and a transfer can never carry a
    // category.
    [Fact]
    public async Task GetHistory_FilteredByPeriodAccountAndCategory_ReturnsMarkedRows()
    {
        Guid accountId = Guid.CreateVersion7();
        Guid categoryId = Guid.CreateVersion7();
        _ledger.HistoryPageResult = new HistoryPage(
            [TransactionRow(accountId, categoryId), TransferRow(accountId)],
            HasNext: true,
            HasPrev: false,
            NextCursor: "opaque-next",
            PrevCursor: null);

        HistoryPage page = await ReadAsync(
            new HistoryFilter(From, To, accountId, categoryId),
            new HistoryPageRequest(After: null, Before: null, Limit: 50));

        _ledger.HistoryFilterSeen.Should().Be(new HistoryFilter(From, To, accountId, categoryId));
        _ledger.HistoryPageSeen.Should().Be(new HistoryPageRequest(null, null, 50));

        page.Items.Should().HaveCount(2);
        page.Items[0].Should().BeOfType<HistoryTransactionRow>()
            .Which.CategoryId.Should().Be(categoryId);
        page.Items[1].Should().BeOfType<HistoryTransferRow>()
            .Which.SourceAccountId.Should().Be(accountId);
        page.HasNext.Should().BeTrue();
        page.NextCursor.Should().Be("opaque-next");
    }

    // The question was valid and the answer is "nothing" — that is not a failure.
    [Fact]
    public async Task GetHistory_EmptyPeriod_ReturnsEmptyPageNotFailure()
    {
        HistoryPage page = await ReadAsync(
            new HistoryFilter(From, To, AccountId: null, CategoryId: null),
            new HistoryPageRequest(After: null, Before: null, Limit: 50));

        page.Items.Should().BeEmpty();
        page.HasNext.Should().BeFalse();
        page.HasPrev.Should().BeFalse();
        page.NextCursor.Should().BeNull();
        page.PrevCursor.Should().BeNull();
    }

    // Every filter is optional: with none set, the whole history is asked for.
    [Fact]
    public async Task GetHistory_WithNoFilter_AsksForTheWholeHistory()
    {
        await ReadAsync(
            new HistoryFilter(From: null, To: null, AccountId: null, CategoryId: null),
            new HistoryPageRequest(After: null, Before: null, Limit: 50));

        _ledger.HistoryFilterSeen.Should().Be(new HistoryFilter(null, null, null, null));
    }

    // Only one end given is a legal half-open ask, so there is nothing to contradict.
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task GetHistory_WithOneEndOfThePeriod_IsAccepted(bool hasFrom, bool hasTo)
    {
        HistoryPage page = await ReadAsync(
            new HistoryFilter(hasFrom ? From : null, hasTo ? To : null, null, null),
            new HistoryPageRequest(After: null, Before: null, Limit: 50));

        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetHistory_FromAfterTo_IsRefused()
    {
        Func<Task> read = () => ReadAsync(
            new HistoryFilter(To, From, AccountId: null, CategoryId: null),
            new HistoryPageRequest(After: null, Before: null, Limit: 50));

        var refusal = (await read.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("History.InvalidFilter");
        refusal.Message.Should().Be("'from' must not be after 'to'.");
        _ledger.HistoryFilterSeen.Should().BeNull();
    }

    // The page size runs from 1 to 200, and defaults to 50.
    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public async Task GetHistory_LimitOutOfRange_IsRefused(int limit)
    {
        Func<Task> read = () => ReadAsync(
            new HistoryFilter(null, null, null, null),
            new HistoryPageRequest(After: null, Before: null, limit));

        var refusal = (await read.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("History.InvalidFilter");
        refusal.Message.Should().Be("'limit' must be between 1 and 200.");
        _ledger.HistoryPageSeen.Should().BeNull();
    }

    [Theory]
    [InlineData(HistoryPageRequest.MinLimit)]
    [InlineData(HistoryPageRequest.DefaultLimit)]
    [InlineData(HistoryPageRequest.MaxLimit)]
    public async Task GetHistory_LimitAtTheEdges_IsAccepted(int limit)
    {
        await ReadAsync(
            new HistoryFilter(null, null, null, null),
            new HistoryPageRequest(After: null, Before: null, limit));

        _ledger.HistoryPageSeen!.Limit.Should().Be(limit);
    }

    // A cursor is opaque: it is handed over unread, never parsed or rebuilt.
    [Fact]
    public async Task GetHistory_WithACursor_HandsItOverUnchanged()
    {
        await ReadAsync(
            new HistoryFilter(null, null, null, null),
            new HistoryPageRequest(After: "opaque-after", Before: null, Limit: 50));

        _ledger.HistoryPageSeen!.After.Should().Be("opaque-after");
    }

    [Fact]
    public async Task GetHistory_WhenCalled_PassesTheCancellationTokenThrough()
    {
        using var cancellation = new CancellationTokenSource();

        await new GetHistory(_ledger).ExecuteAsync(
            new HistoryFilter(null, null, null, null),
            new HistoryPageRequest(After: null, Before: null, Limit: 50),
            cancellation.Token);

        _ledger.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
    }

    private static HistoryTransactionRow TransactionRow(Guid accountId, Guid categoryId) =>
        new(
            Guid.CreateVersion7(),
            new DateOnly(2026, 10, 29),
            new Money(42.5000m),
            accountId,
            "Cash wallet",
            categoryId,
            "Groceries",
            CategoryKind.Expense,
            "Weekly shop",
            Now);

    private static HistoryTransferRow TransferRow(Guid sourceAccountId) =>
        new(
            Guid.CreateVersion7(),
            new DateOnly(2026, 10, 28),
            new Money(200.0000m),
            sourceAccountId,
            "Cash wallet",
            Guid.CreateVersion7(),
            "Savings jar",
            Now);

    private Task<HistoryPage> ReadAsync(HistoryFilter filter, HistoryPageRequest page) =>
        new GetHistory(_ledger).ExecuteAsync(filter, page, CancellationToken.None);
}
