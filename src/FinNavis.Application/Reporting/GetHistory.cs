using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Reporting;

/// <summary>
/// Reads one page of recorded movements, transactions and transfers together (AC-16, AC-17,
/// sad.md §6 flow 8).
/// </summary>
/// <remarks>
/// <b>Every filter is optional.</b> A request with none asks for the whole history. Only the pair
/// of days is checked here, because only that pair can contradict itself.
/// <para>
/// <b>An empty period is an empty page, never a failure</b> (AC-17). A period the owner recorded
/// nothing in is an ordinary answer — the question was valid and the answer is "nothing".
/// </para>
/// <para>
/// <b>No entity is loaded</b> (ADR-0004). The page is read rows from one <c>UNION ALL</c>, so
/// nothing here touches a repository and nothing is written.
/// </para>
/// </remarks>
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
            throw new DomainRuleViolationException(
                "History.InvalidFilter",
                "'from' must not be after 'to'.");
        }
    }

    private static void RequireLimitInRange(int limit)
    {
        if (limit is < HistoryPageRequest.MinLimit or > HistoryPageRequest.MaxLimit)
        {
            throw new DomainRuleViolationException(
                "History.InvalidFilter",
                $"'limit' must be between {HistoryPageRequest.MinLimit} and {HistoryPageRequest.MaxLimit}.");
        }
    }
}
