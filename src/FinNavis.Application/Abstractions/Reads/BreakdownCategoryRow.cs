using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Abstractions.Reads;

public sealed record BreakdownCategoryRow(
    Guid CategoryId,
    string CategoryName,
    CategoryKind Kind,
    Guid? CategoryGroupId,
    string? CategoryGroupName,
    Money Total);
