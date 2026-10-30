using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.UnitTests.Fakes;

/// <summary>
/// An in-memory stand-in for the read path. It reads the accounts out of
/// <see cref="FakeAccountRepository"/>, so an account that was just created shows up with the
/// balance of no movements — zero — without a test having to say so (AC-07).
/// </summary>
/// <remarks>
/// The history page and the breakdown answer with whatever the test put in them, and record what
/// they were asked for — those two use cases are pass-through and assembly, so what matters is
/// that the filters arrive unchanged and the rows are assembled right.
/// <para>
/// A use case that never reads balances passes no repository and leaves the ledger empty.
/// </para>
/// <para>
/// Given the transaction and category stores it also sums the recorded movements into each
/// balance, the way ADR-0002 says a balance is produced: nothing is stored, and the direction of
/// an entry comes from its category's side rather than from a sign on the amount
/// (<c>data-model.md</c> decision 3). Given the transfer store it sums those too, taking the
/// amount off the source and putting it on the destination. That is what lets a test read a
/// balance move after a recording instead of being told it did.
/// </para>
/// </remarks>
internal sealed class FakeLedgerQueries(
    FakeAccountRepository? accounts = null,
    FakeTransactionRepository? transactions = null,
    FakeCategoryRepository? categories = null,
    FakeTransferRepository? transfers = null) : ILedgerQueries
{
    private readonly FakeAccountRepository _accounts = accounts ?? new FakeAccountRepository();
    private readonly FakeTransactionRepository _transactions = transactions ?? new FakeTransactionRepository();
    private readonly FakeCategoryRepository _categories = categories ?? new FakeCategoryRepository();
    private readonly FakeTransferRepository _transfers = transfers ?? new FakeTransferRepository();

    /// <summary>What each account holds. An account with no entry here holds nothing.</summary>
    public Dictionary<Guid, decimal> Balances { get; } = [];

    /// <summary>The accounts at least one transaction or transfer names (AC-09).</summary>
    public HashSet<Guid> AccountsWithRecordedHistory { get; } = [];

    /// <summary>The categories at least one transaction names (AC-12).</summary>
    public HashSet<Guid> CategoriesWithRecordedHistory { get; } = [];

    /// <summary>Every token the use case passed in, so a test can check it was carried through.</summary>
    public List<CancellationToken> TokensSeen { get; } = [];

    /// <summary>The page history answers with. An empty one by default, which AC-17 asks for.</summary>
    public HistoryPage HistoryPageResult { get; set; } =
        new([], HasNext: false, HasPrev: false, NextCursor: null, PrevCursor: null);

    /// <summary>The filter the use case handed over, so a test can check it arrived unchanged.</summary>
    public HistoryFilter? HistoryFilterSeen { get; private set; }

    /// <summary>The page request the use case handed over, for the same reason.</summary>
    public HistoryPageRequest? HistoryPageSeen { get; private set; }

    /// <summary>The raw rows the breakdown is assembled from. Empty means the period recorded nothing.</summary>
    public List<BreakdownCategoryRow> BreakdownRows { get; } = [];

    /// <summary>The period the use case handed over, so a test can check the range it built.</summary>
    public DateRange? BreakdownPeriodSeen { get; private set; }

    /// <summary>The account filter the use case handed over.</summary>
    public Guid? BreakdownAccountIdSeen { get; private set; }

    public Task<IReadOnlyList<AccountBalanceRow>> GetAccountBalancesAsync(
        Guid? accountId,
        CancellationToken cancellationToken)
    {
        TokensSeen.Add(cancellationToken);

        IReadOnlyList<AccountBalanceRow> rows =
        [
            .. _accounts.Stored
                .Where(account => accountId is null || account.Id == accountId)
                .OrderBy(account => account.Name, StringComparer.Ordinal)
                .Select(ToRow),
        ];

        return Task.FromResult(rows);
    }

    public Task<bool> AnyMovementNamesAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        TokensSeen.Add(cancellationToken);

        return Task.FromResult(AccountsWithRecordedHistory.Contains(accountId));
    }

    public Task<bool> AnyTransactionNamesCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        TokensSeen.Add(cancellationToken);

        return Task.FromResult(CategoriesWithRecordedHistory.Contains(categoryId));
    }

    public Task<HistoryPage> GetHistoryPageAsync(
        HistoryFilter filter,
        HistoryPageRequest page,
        CancellationToken cancellationToken)
    {
        TokensSeen.Add(cancellationToken);
        HistoryFilterSeen = filter;
        HistoryPageSeen = page;

        return Task.FromResult(HistoryPageResult);
    }

    public Task<IReadOnlyList<BreakdownCategoryRow>> GetBreakdownAsync(
        DateRange period,
        Guid? accountId,
        CancellationToken cancellationToken)
    {
        TokensSeen.Add(cancellationToken);
        BreakdownPeriodSeen = period;
        BreakdownAccountIdSeen = accountId;

        return Task.FromResult<IReadOnlyList<BreakdownCategoryRow>>([.. BreakdownRows]);
    }

    private AccountBalanceRow ToRow(Account account) =>
        new(
            account.Id,
            account.Name,
            new Money(Balances.GetValueOrDefault(account.Id) + RecordedMovement(account.Id)));

    // The three-part sum of ADR-0001: the transactions on the account, minus the transfers out of
    // it, plus the transfers into it.
    private decimal RecordedMovement(Guid accountId) =>
        _transactions.Stored.Where(transaction => transaction.AccountId == accountId).Sum(Signed)
        - _transfers.Stored.Where(transfer => transfer.SourceAccountId == accountId)
            .Sum(transfer => transfer.Amount.Amount)
        + _transfers.Stored.Where(transfer => transfer.DestinationAccountId == accountId)
            .Sum(transfer => transfer.Amount.Amount);

    // The amount is always positive; the category's side is what makes the entry add or subtract.
    private decimal Signed(Transaction transaction)
    {
        Category category = _categories.Stored.SingleOrDefault(
                candidate => candidate.Id == transaction.CategoryId)
            ?? throw new InvalidOperationException(
                $"Seed the category {transaction.CategoryId} before reading a balance that sums it.");

        return category.Kind is CategoryKind.Expense ? -transaction.Amount.Amount : transaction.Amount.Amount;
    }
}
