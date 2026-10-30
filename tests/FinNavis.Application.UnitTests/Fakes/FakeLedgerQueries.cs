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
/// The history page and the breakdown are not part of the account or category use cases. They
/// refuse rather than answer, so a test that reaches one by accident fails loudly instead of
/// reading a made-up figure.
/// <para>
/// A use case that never reads balances passes no repository and leaves the ledger empty.
/// </para>
/// <para>
/// Given the transaction and category stores it also sums the recorded movements into each
/// balance, the way ADR-0002 says a balance is produced: nothing is stored, and the direction of
/// an entry comes from its category's side rather than from a sign on the amount
/// (<c>data-model.md</c> decision 3). That is what lets a test read a balance fall after a
/// recording instead of being told it did.
/// </para>
/// </remarks>
internal sealed class FakeLedgerQueries(
    FakeAccountRepository? accounts = null,
    FakeTransactionRepository? transactions = null,
    FakeCategoryRepository? categories = null) : ILedgerQueries
{
    private readonly FakeAccountRepository _accounts = accounts ?? new FakeAccountRepository();
    private readonly FakeTransactionRepository _transactions = transactions ?? new FakeTransactionRepository();
    private readonly FakeCategoryRepository _categories = categories ?? new FakeCategoryRepository();

    /// <summary>What each account holds. An account with no entry here holds nothing.</summary>
    public Dictionary<Guid, decimal> Balances { get; } = [];

    /// <summary>The accounts at least one transaction or transfer names (AC-09).</summary>
    public HashSet<Guid> AccountsWithRecordedHistory { get; } = [];

    /// <summary>The categories at least one transaction names (AC-12).</summary>
    public HashSet<Guid> CategoriesWithRecordedHistory { get; } = [];

    /// <summary>Every token the use case passed in, so a test can check it was carried through.</summary>
    public List<CancellationToken> TokensSeen { get; } = [];

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
        CancellationToken cancellationToken) =>
        throw new NotSupportedException("No use case tested here reads history.");

    public Task<IReadOnlyList<BreakdownCategoryRow>> GetBreakdownAsync(
        DateRange period,
        Guid? accountId,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException("No use case tested here reads the breakdown.");

    private AccountBalanceRow ToRow(Account account) =>
        new(
            account.Id,
            account.Name,
            new Money(Balances.GetValueOrDefault(account.Id) + RecordedMovement(account.Id)));

    private decimal RecordedMovement(Guid accountId) =>
        _transactions.Stored
            .Where(transaction => transaction.AccountId == accountId)
            .Sum(Signed);

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
