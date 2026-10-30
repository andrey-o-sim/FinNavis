using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Reporting;

public sealed record BreakdownGroupTotal(
    Guid? CategoryGroupId,
    string Name,
    Money Total,
    IReadOnlyList<BreakdownCategoryTotal> Categories);
