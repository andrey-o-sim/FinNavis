using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Reporting;

public sealed class GetHistory(ILedgerQueries ledger)
{
    public Task<HistoryPage> ExecuteAsync(
        HistoryFilter filter,
        HistoryPageRequest page,
        CancellationToken cancellationToken)
    {
        RequireDaysInOrder(filter);
        RequireLimitInRange(page.Limit);

        return ledger.GetHistoryPageAsync(filter, page, cancellationToken);
    }

    // Each end is optional on its own, so there is only something to check when both are given.
    private static void RequireDaysInOrder(HistoryFilter filter)
    {
        if (filter.From is not DateOnly from || filter.To is not DateOnly to)
        {
            return;
        }

        if (!DateRange.TryCreate(from, to, out _))
        {
            throw HistoryErrors.DaysOutOfOrder();
        }
    }

    private static void RequireLimitInRange(int limit)
    {
        if (limit is < HistoryPageRequest.MinLimit or > HistoryPageRequest.MaxLimit)
        {
            throw HistoryErrors.LimitOutOfRange();
        }
    }
}
