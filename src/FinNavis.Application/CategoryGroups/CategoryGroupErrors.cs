using FinNavis.Application.Common;
using FinNavis.Application.Reporting;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;

namespace FinNavis.Application.CategoryGroups;

internal static class CategoryGroupErrors
{
    public static DomainRuleViolationException NotFound(Guid categoryGroupId) =>
        new("CategoryGroup.NotFound", $"No category group in the ledger has id {categoryGroupId}.");

    public static DomainRuleViolationException DuplicateName(string name, CategoryKind kind) =>
        new(
            "CategoryGroup.DuplicateName",
            $"An {kind.AsLedgerSide()} category group named '{name}' already exists.");

    public static DomainRuleViolationException NameReserved() =>
        new(
            "CategoryGroup.NameReserved",
            $"'{DefaultGroup.Name}' is the breakdown's own bucket, so a group cannot carry that name.");
}
