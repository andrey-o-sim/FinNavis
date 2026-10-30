using FinNavis.Application.Common;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;

namespace FinNavis.Application.Categories;

internal static class CategoryErrors
{
    public static DomainRuleViolationException NotFound(Guid categoryId) =>
        new("Category.NotFound", $"No category in the ledger has id {categoryId}.");

    public static DomainRuleViolationException GroupNotFound(Guid categoryGroupId) =>
        new("CategoryGroup.NotFound", $"No category group in the ledger has id {categoryGroupId}.");

    public static DomainRuleViolationException DuplicateName(string name, CategoryKind kind) =>
        new(
            "Category.DuplicateName",
            $"An {kind.AsLedgerSide()} category named '{name}' already exists.");

    public static DomainRuleViolationException HasRecordedHistory() =>
        new(
            "Category.HasRecordedHistory",
            "Recorded history depends on this category, so it cannot be deleted.");
}
