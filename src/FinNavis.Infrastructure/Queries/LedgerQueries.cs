using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Infrastructure.Queries;

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
