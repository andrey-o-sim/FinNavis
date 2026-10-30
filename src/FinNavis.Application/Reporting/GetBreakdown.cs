using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Reporting;

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
                // come back in a different order.
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
