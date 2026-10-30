using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Abstractions;

public interface ILedgerQueries
{
    Task<IReadOnlyList<AccountBalanceRow>> GetAccountBalancesAsync(
        Guid? accountId,
        CancellationToken cancellationToken);

    Task<bool> AnyMovementNamesAccountAsync(Guid accountId, CancellationToken cancellationToken);

    Task<bool> AnyTransactionNamesCategoryAsync(Guid categoryId, CancellationToken cancellationToken);

    Task<HistoryPage> GetHistoryPageAsync(
        HistoryFilter filter,
        HistoryPageRequest page,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<BreakdownCategoryRow>> GetBreakdownAsync(
        DateRange period,
        Guid? accountId,
        CancellationToken cancellationToken);
}
