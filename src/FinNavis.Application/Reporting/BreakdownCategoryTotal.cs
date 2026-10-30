using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Reporting;

public sealed record BreakdownCategoryTotal(Guid CategoryId, string Name, Money Total);
