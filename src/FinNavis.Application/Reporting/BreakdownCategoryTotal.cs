using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Reporting;

/// <summary>
/// One category's total for the period.
/// </summary>
/// <param name="CategoryId">The category the figure belongs to.</param>
/// <param name="Name">The category's name.</param>
/// <param name="Total">The sum of its transactions in the period. Always above zero.</param>
public sealed record BreakdownCategoryTotal(Guid CategoryId, string Name, Money Total);
