using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Infrastructure.Queries;

/// <summary>
/// The adapter behind <see cref="ILedgerQueries"/>: one port, three statements that have nothing
/// in common beyond belonging to the read path (sad.md §5, ADR-0004).
/// </summary>
/// <remarks>
/// The port is one interface because a use case asks the ledger for a figure, not three different
/// services. The statements are three classes because a balance, a history page and a period
/// breakdown are unrelated pieces of SQL, and keeping them apart is what stops one file from
/// holding every query in the feature.
/// </remarks>
public sealed class LedgerQueries(
    LedgerBalanceQueries balances,
    LedgerHistoryQueries history,
    LedgerBreakdownQueries breakdown) : ILedgerQueries
{
    public Task<IReadOnlyList<AccountBalanceRow>> GetAccountBalancesAsync(
        Guid? accountId,
        CancellationToken cancellationToken) =>
        balances.GetAccountBalancesAsync(accountId, cancellationToken);

    public Task<bool> AnyMovementNamesAccountAsync(Guid accountId, CancellationToken cancellationToken) =>
        balances.AnyMovementNamesAccountAsync(accountId, cancellationToken);

    public Task<bool> AnyTransactionNamesCategoryAsync(Guid categoryId, CancellationToken cancellationToken) =>
        balances.AnyTransactionNamesCategoryAsync(categoryId, cancellationToken);

    public Task<HistoryPage> GetHistoryPageAsync(
        HistoryFilter filter,
        HistoryPageRequest page,
        CancellationToken cancellationToken) =>
        history.GetHistoryPageAsync(filter, page, cancellationToken);

    public Task<IReadOnlyList<BreakdownCategoryRow>> GetBreakdownAsync(
        DateRange period,
        Guid? accountId,
        CancellationToken cancellationToken) =>
        breakdown.GetBreakdownAsync(period, accountId, cancellationToken);
}
