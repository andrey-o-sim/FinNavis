using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Reporting;

public sealed record BreakdownSide(Money Total, IReadOnlyList<BreakdownGroupTotal> Groups);
