using FinNavis.Domain.Errors;

namespace FinNavis.Application.Import;

internal static class ImportErrors
{
    public static DomainRuleViolationException MissingRequiredColumn(IReadOnlyList<string> missingColumnNames) =>
        new(
            "Import.MissingRequiredColumn",
            "This file does not say the date or the amount, so nothing can be recorded from it.",
            new Dictionary<string, object> { ["missing"] = missingColumnNames });

    public static DomainRuleViolationException CategoryNotFound(string sourceCategory, Guid targetCategoryId) =>
        new(
            "Category.NotFound",
            "There is no category with that name, so a rule cannot point at it.",
            new Dictionary<string, object>
            {
                ["source_category"] = sourceCategory,
                ["target_category_id"] = targetCategoryId,
            });
}
