using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Reporting;

/// <summary>
/// Totals a period and reports group figures and category figures in one answer (AC-18, AC-19,
/// AC-20, sad.md §6 flow 3).
/// </summary>
/// <remarks>
/// <b>The aggregation runs in PostgreSQL</b> (ADR-0004). What is left here is the assembly: the
/// raw rows are grouped, each side is totalled, and every category belonging to no group is put
/// into the Default group bucket.
/// <para>
/// <b>The Default group is produced, never stored.</b> There is no such row and no category is
/// ever assigned to it (sad.md §12). A category reaches the bucket by having no group at all.
/// </para>
/// <para>
/// <b>AC-19 holds because one pass does both.</b> A side's total and its group totals are summed
/// from the same rows, so the group figures plus the Default figure cannot fail to add up to the
/// side figure.
/// </para>
/// <para>
/// <b>Spending and earning are never added together</b> (AC-18). The two sides are split before
/// anything is summed.
/// </para>
/// <para>
/// <b>No transfer is counted</b> (AC-20). The query never reads <c>transfers</c>, so this is
/// structural rather than a filter here (ADR-0001).
/// </para>
/// </remarks>
public sealed class GetBreakdown(ILedgerQueries ledger)
{
    public async Task<Breakdown> ExecuteAsync(
        DateOnly from,
        DateOnly to,
        Guid? accountId,
        CancellationToken cancellationToken)
    {
        if (!DateRange.TryCreate(from, to, out DateRange period))
        {
            throw new DomainRuleViolationException(
                "Breakdown.InvalidPeriod",
                "'from' must not be after 'to'.");
        }

        IReadOnlyList<BreakdownCategoryRow> rows =
            await ledger.GetBreakdownAsync(period, accountId, cancellationToken);

        return new Breakdown(
            period,
            accountId,
            SideOf(rows, CategoryKind.Expense),
            SideOf(rows, CategoryKind.Income));
    }

    private static BreakdownSide SideOf(IReadOnlyList<BreakdownCategoryRow> rows, CategoryKind kind)
    {
        BreakdownCategoryRow[] ofThisSide = [.. rows.Where(row => row.Kind == kind)];

        IReadOnlyList<BreakdownGroupTotal> groups =
        [
            .. ofThisSide
                .GroupBy(row => row.CategoryGroupId)
                .Select(ToGroupTotal)
                // The Default group last, then by name, so two reads of the same period cannot
                // come back in a different order (QG-3).
                .OrderBy(group => group.CategoryGroupId is null)
                .ThenBy(group => group.Name, StringComparer.Ordinal),
        ];

        return new BreakdownSide(Total(groups.Select(group => group.Total)), groups);
    }

    private static BreakdownGroupTotal ToGroupTotal(IGrouping<Guid?, BreakdownCategoryRow> group)
    {
        IReadOnlyList<BreakdownCategoryTotal> categories =
        [
            .. group
                .Select(row => new BreakdownCategoryTotal(row.CategoryId, row.CategoryName, row.Total))
                .OrderBy(category => category.Name, StringComparer.Ordinal),
        ];

        // A row with no group carries no group name, so the bucket is named here.
        string name = group.First().CategoryGroupName ?? DefaultGroup.Name;

        return new BreakdownGroupTotal(
            group.Key,
            name,
            Total(categories.Select(category => category.Total)),
            categories);
    }

    private static Money Total(IEnumerable<Money> figures) =>
        figures.Aggregate(Money.Zero, (sum, figure) => sum + figure);
}
