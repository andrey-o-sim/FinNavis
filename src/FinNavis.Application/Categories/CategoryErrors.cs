using FinNavis.Application.Common;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;

namespace FinNavis.Application.Categories;

/// <summary>
/// The refusals the category use cases raise, in one place.
/// </summary>
/// <remarks>
/// Five use cases share four of these, and two of them name a group rather than a category, so
/// writing them once is what keeps the wording identical wherever the same rule refuses. The
/// wording itself is the PRD acceptance criterion, not invented (sad.md §8).
/// <para>
/// <b><c>Category.GroupNotFound</c> does not exist.</b> It was merged into
/// <c>CategoryGroup.NotFound</c> — one refusal, one code — and <c>docs/adr/0004</c> made that the
/// rule: the module of a <c>*.NotFound</c> code names the aggregate that was not found.
/// </para>
/// </remarks>
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
