using FinNavis.Domain.Errors;

namespace FinNavis.Application.Import;

internal static class ImportErrors
{
    private const string MissingRequiredColumnCode = "Import.MissingRequiredColumn";

    private const string InvalidPagingCode = "Import.InvalidPaging";

    public static DomainRuleViolationException RunNotFound(Guid importRunId) =>
        new("Import.NotFound", $"No load in this installation has id {importRunId}.");

    public static DomainRuleViolationException MissingRequiredColumn(IReadOnlyList<string> missingColumnNames) =>
        new(
            MissingRequiredColumnCode,
            "This file does not say the date or the amount, so nothing can be recorded from it.",
            new Dictionary<string, object> { ["missing"] = missingColumnNames });

    public static DomainRuleViolationException MissingRequiredExportColumn(IReadOnlyList<string> missingColumnNames) =>
        new(
            MissingRequiredColumnCode,
            "This file does not say the date or the amount, or the account each row belongs to, "
                + "so nothing can be recorded from it.",
            new Dictionary<string, object> { ["missing"] = missingColumnNames });

    public static DomainRuleViolationException FileNotReadable(Exception readFailure) =>
        new(
            "Import.FileNotReadable",
            "This file could not be read as a statement, so nothing was recorded.",
            readFailure);

    public static DomainRuleViolationException PagingLimitOutOfRange(int limit, int minLimit, int maxLimit) =>
        new(
            InvalidPagingCode,
            $"'limit' must be between {minLimit} and {maxLimit}.",
            new Dictionary<string, object> { ["limit"] = limit });

    public static DomainRuleViolationException PagingCursorNotMinted() =>
        new(InvalidPagingCode, "That is not a cursor this server handed out.");

    public static DomainRuleViolationException AccountNotFound(Guid accountId) =>
        new(
            "Account.NotFound",
            "You do not have an account with that name, so there was nothing to load this file onto. "
                + "Nothing was recorded and no account was created.",
            new Dictionary<string, object> { ["account_id"] = accountId });

    public static DomainRuleViolationException MappingProfileNotConfirmed(string shapeKey) =>
        new(
            "Import.MappingProfileNotConfirmed",
            "There are no confirmed rules for this kind of file yet, so its rows had nowhere to go. "
                + "Nothing was recorded.",
            new Dictionary<string, object> { ["shape_key"] = shapeKey });

    public static DomainRuleViolationException NotRecorded(Exception fault) =>
        new(
            "Import.NotRecorded",
            "The load stopped before it finished, so nothing from it was recorded and every balance is unchanged.",
            fault);

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
