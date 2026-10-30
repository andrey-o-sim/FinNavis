using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Reporting;

/// <summary>
/// One group's total for the period, with the categories that make it up.
/// </summary>
/// <param name="CategoryGroupId">
/// The group, or <c>null</c> for the Default group — which this report produces and which is not a
/// row anybody could name (sad.md §12).
/// </param>
/// <param name="Name">The group's name, or <see cref="DefaultGroup.Name"/>.</param>
/// <param name="Total">The sum of the categories below.</param>
/// <param name="Categories">The categories that recorded something in the period.</param>
public sealed record BreakdownGroupTotal(
    Guid? CategoryGroupId,
    string Name,
    Money Total,
    IReadOnlyList<BreakdownCategoryTotal> Categories);
