using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Reporting;

public sealed record Breakdown(
    DateRange Period,
    Guid? AccountId,
    BreakdownSide Expense,
    BreakdownSide Income);
