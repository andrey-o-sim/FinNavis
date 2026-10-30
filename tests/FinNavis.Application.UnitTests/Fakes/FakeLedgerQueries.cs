using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.Entities;
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
/// </remarks>
internal sealed class FakeLedgerQueries(FakeAccountRepository? accounts = null) : ILedgerQueries
{
    private readonly FakeAccountRepository _accounts = accounts ?? new FakeAccountRepository();

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
        throw new NotSupportedException("The account use cases never read history.");

    public Task<IReadOnlyList<BreakdownCategoryRow>> GetBreakdownAsync(
        DateRange period,
        Guid? accountId,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException("The account use cases never read the breakdown.");

    private AccountBalanceRow ToRow(Account account) =>
        new(account.Id, account.Name, new Money(Balances.GetValueOrDefault(account.Id)));
}
