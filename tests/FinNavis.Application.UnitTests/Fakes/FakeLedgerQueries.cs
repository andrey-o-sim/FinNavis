using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.UnitTests.Fakes;

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

    public Dictionary<Guid, decimal> Balances { get; } = [];

    public HashSet<Guid> AccountsWithRecordedHistory { get; } = [];

    public HashSet<Guid> CategoriesWithRecordedHistory { get; } = [];

    public List<CancellationToken> TokensSeen { get; } = [];

    public HistoryPage HistoryPageResult { get; set; } =
        new([], HasNext: false, HasPrev: false, NextCursor: null, PrevCursor: null);

    public HistoryFilter? HistoryFilterSeen { get; private set; }

    public HistoryPageRequest? HistoryPageSeen { get; private set; }

    public List<BreakdownCategoryRow> BreakdownRows { get; } = [];

    public DateRange? BreakdownPeriodSeen { get; private set; }

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

    // A three-part sum: the transactions on the account, minus the transfers out of it, plus
    // the transfers into it.
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
