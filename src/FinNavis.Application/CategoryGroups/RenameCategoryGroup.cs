using FinNavis.Application.Abstractions;
using FinNavis.Application.Common;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;

namespace FinNavis.Application.CategoryGroups;

public sealed class RenameCategoryGroup(ICategoryGroupRepository categoryGroups, IUnitOfWork unitOfWork)
{
    public async Task<CategoryGroup> ExecuteAsync(
        Guid categoryGroupId,
        string name,
        CancellationToken cancellationToken)
    {
        CategoryGroup group = await categoryGroups.GetByIdAsync(categoryGroupId, cancellationToken)
            ?? throw NotFound(categoryGroupId);

        // The stored name is trimmed, so the comparison and the check have to use a trimmed name.
        string requestedName = (name ?? string.Empty).Trim();

        // The check answers true for the group's own row, so keeping its current name is not a
        // clash — the refusal is about another group on that side already carrying the name.
        bool nameChanges = !string.Equals(requestedName, group.Name, StringComparison.Ordinal);

        if (nameChanges
            && await categoryGroups.ExistsWithNameAsync(requestedName, group.Kind, cancellationToken))
        {
            throw DuplicateName(requestedName, group.Kind);
        }

        group.Rename(requestedName);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return group;
    }

    private static DomainRuleViolationException NotFound(Guid categoryGroupId) =>
        new("CategoryGroup.NotFound", $"No category group in the ledger has id {categoryGroupId}.");

    private static DomainRuleViolationException DuplicateName(string name, CategoryKind kind) =>
        new(
            "CategoryGroup.DuplicateName",
            $"An {kind.AsLedgerSide()} category group named '{name}' already exists.");
}
