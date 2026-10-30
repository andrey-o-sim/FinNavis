using System.Globalization;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;
using FinNavis.Infrastructure.IntegrationTests.Repositories;
using FinNavis.Infrastructure.Persistence;
using FinNavis.Infrastructure.Queries;
using FinNavis.Infrastructure.Repositories;

namespace FinNavis.Infrastructure.IntegrationTests.Queries;

[Collection(DatabaseCollection.Name)]
public sealed class HistoryQueryTests : IAsyncLifetime
{
    private static readonly HistoryFilter NoFilter = new(From: null, To: null, AccountId: null, CategoryId: null);

    private readonly PostgresFixture _database;

    public HistoryQueryTests(PostgresFixture database) => _database = database;

    public Task InitializeAsync() => _database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // A page carries both kinds of movement, each marked as what it is, and a transaction shows
    // the note the owner wrote.
    [Fact]
    public async Task History_FilteredByPeriodAccountAndCategory_ReturnsMatchingRows()
    {
        Account wallet = LedgerData.NewAccount("Cash wallet");
        Account jar = LedgerData.NewAccount("Savings jar");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);
        Category books = LedgerData.NewCategory("Books", CategoryKind.Expense);

        Transaction inPeriod = Movement(wallet.Id, groceries.Id, 10m, "2026-03-10", "Bread");
        Transaction outsidePeriod = Movement(wallet.Id, groceries.Id, 20m, "2026-04-02", "Later");
        Transaction otherCategory = Movement(wallet.Id, books.Id, 30m, "2026-03-11", "Novel");
        Transaction otherAccount = Movement(jar.Id, groceries.Id, 40m, "2026-03-12", "Elsewhere");

        await StoreAsync(
            [wallet, jar],
            [groceries, books],
            [inPeriod, outsidePeriod, otherCategory, otherAccount],
            []);

        HistoryPage page = await ReadPageAsync(
            new HistoryFilter(Day("2026-03-01"), Day("2026-03-31"), wallet.Id, groceries.Id),
            Request());

        page.Items.Should().ContainSingle();

        var row = page.Items[0].Should().BeOfType<HistoryTransactionRow>().Subject;
        row.Id.Should().Be(inPeriod.Id);
        row.AccountName.Should().Be("Cash wallet");
        row.CategoryName.Should().Be("Groceries");
        row.CategoryKind.Should().Be(CategoryKind.Expense);
        row.Note.Should().Be("Bread");
        row.Amount.Should().Be(new Money(10m));
    }

    [Fact]
    public async Task History_WithNoFilter_MarksEveryRowAsTransactionOrTransfer()
    {
        Account wallet = LedgerData.NewAccount("Cash wallet");
        Account jar = LedgerData.NewAccount("Savings jar");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);

        Transaction spent = Movement(wallet.Id, groceries.Id, 10m, "2026-03-10", "Bread");
        Transfer moved = Move(wallet.Id, jar.Id, 55m, "2026-03-11");

        await StoreAsync([wallet, jar], [groceries], [spent], [moved]);

        HistoryPage page = await ReadPageAsync(NoFilter, Request());

        page.Items.Should().HaveCount(2);

        // Newest first, so the transfer of the 11th comes before the transaction of the 10th.
        var transfer = page.Items[0].Should().BeOfType<HistoryTransferRow>().Subject;
        transfer.SourceAccountName.Should().Be("Cash wallet");
        transfer.DestinationAccountName.Should().Be("Savings jar");
        transfer.Amount.Should().Be(new Money(55m));

        page.Items[1].Should().BeOfType<HistoryTransactionRow>().Which.Id.Should().Be(spent.Id);
    }

    [Fact]
    public async Task History_CategoryFilter_ReturnsNoTransfer()
    {
        Account wallet = LedgerData.NewAccount("Cash wallet");
        Account jar = LedgerData.NewAccount("Savings jar");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);

        Transaction spent = Movement(wallet.Id, groceries.Id, 10m, "2026-03-10", note: null);

        await StoreAsync([wallet, jar], [groceries], [spent], [Move(wallet.Id, jar.Id, 55m, "2026-03-11")]);

        HistoryPage page = await ReadPageAsync(
            new HistoryFilter(From: null, To: null, AccountId: null, CategoryId: groceries.Id),
            Request());

        page.Items.Should().ContainSingle().Which.Should().BeOfType<HistoryTransactionRow>();
    }

    // A period that recorded nothing is an empty page, not a failure.
    [Fact]
    public async Task History_EmptyPeriod_ReturnsEmptyPageNotFailure()
    {
        Account wallet = LedgerData.NewAccount("Cash wallet");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);

        await StoreAsync([wallet], [groceries], [Movement(wallet.Id, groceries.Id, 10m, "2026-03-10", null)], []);

        HistoryPage page = await ReadPageAsync(
            new HistoryFilter(Day("2026-01-01"), Day("2026-01-31"), AccountId: null, CategoryId: null),
            Request());

        page.Items.Should().BeEmpty();
        page.HasNext.Should().BeFalse();
        page.HasPrev.Should().BeFalse();
        page.NextCursor.Should().BeNull();
        page.PrevCursor.Should().BeNull();
    }

    // Paging forward then back over the same seeded set: every movement appears exactly once in
    // the forward walk, and the backward walk lands on the same page it came from.
    [Fact]
    public async Task History_PagedForwardThenBack_ReturnsEveryRowOnceWithNoGapAndNoRepeat()
    {
        IReadOnlyList<Guid> newestFirst = await SeedSevenMovementsAsync();

        List<Guid> walked = [];
        string? after = null;

        do
        {
            HistoryPage page = await ReadPageAsync(NoFilter, Request(after: after, limit: 3));

            walked.AddRange(page.Items.Select(item => item.Id));
            after = page.HasNext ? page.NextCursor : null;
        }
        while (after is not null);

        walked.Should().Equal(newestFirst);

        // Walk one page forward, then back from it, and land on the page it came from.
        HistoryPage first = await ReadPageAsync(NoFilter, Request(limit: 3));
        HistoryPage second = await ReadPageAsync(NoFilter, Request(after: first.NextCursor, limit: 3));
        HistoryPage backToFirst = await ReadPageAsync(NoFilter, Request(before: second.PrevCursor, limit: 3));

        second.Items.Select(item => item.Id).Should().Equal(newestFirst.Skip(3).Take(3));
        backToFirst.Items.Select(item => item.Id).Should().Equal(newestFirst.Take(3));
        backToFirst.HasPrev.Should().BeFalse();
        backToFirst.HasNext.Should().BeTrue();
    }

    [Fact]
    public async Task History_PagedForward_MarksTheFirstPageAsHavingNoPrevious()
    {
        await SeedSevenMovementsAsync();

        HistoryPage first = await ReadPageAsync(NoFilter, Request(limit: 3));

        first.HasPrev.Should().BeFalse();
        first.PrevCursor.Should().BeNull();
        first.HasNext.Should().BeTrue();
        first.NextCursor.Should().NotBeNull();

        HistoryPage second = await ReadPageAsync(NoFilter, Request(after: first.NextCursor, limit: 3));

        second.HasPrev.Should().BeTrue();
        second.PrevCursor.Should().NotBeNull();
    }

    // The cursor is a token the server mints. A client that tried to build one out of a row id
    // gets it refused, so the encoding is free to change later.
    [Fact]
    public async Task History_WithARowIdPassedAsACursor_IsRejected()
    {
        IReadOnlyList<Guid> newestFirst = await SeedSevenMovementsAsync();

        Func<Task> read = () => ReadPageAsync(NoFilter, Request(after: newestFirst[0].ToString()));

        await read.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task History_WithACursorFromTheServer_IsNotTheRowIdItPointsAt()
    {
        IReadOnlyList<Guid> newestFirst = await SeedSevenMovementsAsync();

        HistoryPage first = await ReadPageAsync(NoFilter, Request(limit: 3));

        first.NextCursor.Should().NotBe(newestFirst[2].ToString());
        first.NextCursor.Should().NotContain(newestFirst[2].ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(HistoryPageRequest.MaxLimit + 1)]
    public async Task History_WithALimitOutsideTheAllowedRange_IsRejected(int limit)
    {
        await SeedSevenMovementsAsync();

        Func<Task> read = () => ReadPageAsync(NoFilter, Request(limit: limit));

        await read.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task History_WithTheDefaultLimit_ReadsEverySeededMovementInOnePage()
    {
        IReadOnlyList<Guid> newestFirst = await SeedSevenMovementsAsync();

        HistoryPage page = await ReadPageAsync(NoFilter, Request(limit: HistoryPageRequest.DefaultLimit));

        HistoryPageRequest.DefaultLimit.Should().Be(50);
        page.Items.Should().HaveCount(newestFirst.Count);
        page.HasNext.Should().BeFalse();
    }

    [Fact]
    public async Task History_WithBothCursorsGiven_IsRejected()
    {
        await SeedSevenMovementsAsync();

        HistoryPage first = await ReadPageAsync(NoFilter, Request(limit: 3));

        Func<Task> read = () => ReadPageAsync(
            NoFilter,
            Request(after: first.NextCursor, before: first.NextCursor, limit: 3));

        await read.Should().ThrowAsync<ArgumentException>();
    }

    // Seven movements on seven days, so the walk crosses page boundaries at 3, 6 and 7.
    private async Task<IReadOnlyList<Guid>> SeedSevenMovementsAsync()
    {
        Account wallet = LedgerData.NewAccount("Cash wallet");
        Account jar = LedgerData.NewAccount("Savings jar");
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);

        List<Transaction> transactions =
        [
            Movement(wallet.Id, groceries.Id, 1m, "2026-03-01", null),
            Movement(wallet.Id, groceries.Id, 2m, "2026-03-02", null),
            Movement(wallet.Id, groceries.Id, 3m, "2026-03-04", null),
            Movement(wallet.Id, groceries.Id, 4m, "2026-03-06", null),
        ];

        List<Transfer> transfers =
        [
            Move(wallet.Id, jar.Id, 5m, "2026-03-03"),
            Move(wallet.Id, jar.Id, 6m, "2026-03-05"),
            Move(wallet.Id, jar.Id, 7m, "2026-03-07"),
        ];

        await StoreAsync([wallet, jar], [groceries], transactions, transfers);

        return
        [
            transfers[2].Id,
            transactions[3].Id,
            transfers[1].Id,
            transactions[2].Id,
            transfers[0].Id,
            transactions[1].Id,
            transactions[0].Id,
        ];
    }

    private static HistoryPageRequest Request(
        string? after = null,
        string? before = null,
        int limit = HistoryPageRequest.DefaultLimit) =>
        new(after, before, limit);

    private static DateOnly Day(string day) =>
        DateOnly.ParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static Transaction Movement(Guid accountId, Guid categoryId, decimal amount, string day, string? note) =>
        Transaction.Record(
            LedgerData.NewId(),
            accountId,
            categoryId,
            new Money(amount),
            Day(day),
            note,
            DateTimeOffset.UtcNow);

    private static Transfer Move(Guid sourceAccountId, Guid destinationAccountId, decimal amount, string day) =>
        Transfer.Record(
            LedgerData.NewId(),
            sourceAccountId,
            destinationAccountId,
            new Money(amount),
            Day(day),
            DateTimeOffset.UtcNow);

    private async Task<HistoryPage> ReadPageAsync(HistoryFilter filter, HistoryPageRequest page)
    {
        await using var context = _database.CreateDbContext();

        return await new LedgerHistoryQueries(context).GetHistoryPageAsync(filter, page, CancellationToken.None);
    }

    private async Task StoreAsync(
        IReadOnlyList<Account> accounts,
        IReadOnlyList<Category> categories,
        IReadOnlyList<Transaction> transactions,
        IReadOnlyList<Transfer> transfers)
    {
        await using var context = _database.CreateDbContext();

        var accountRepository = new AccountRepository(context);
        foreach (Account account in accounts)
        {
            accountRepository.Add(account);
        }

        var categoryRepository = new CategoryRepository(context);
        foreach (Category category in categories)
        {
            categoryRepository.Add(category);
        }

        var transactionRepository = new TransactionRepository(context);
        foreach (Transaction transaction in transactions)
        {
            transactionRepository.Add(transaction);
        }

        var transferRepository = new TransferRepository(context);
        foreach (Transfer transfer in transfers)
        {
            transferRepository.Add(transfer);
        }

        await new UnitOfWork(context).SaveChangesAsync(CancellationToken.None);
    }
}
