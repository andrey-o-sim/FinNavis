using FinNavis.Application.Abstractions;
using FinNavis.Application.Common;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;

namespace FinNavis.Application.CategoryGroups;

/// <summary>
/// Gives a group a new name and keeps its identity and its side (AC-28, AC-31, AC-32).
/// </summary>
/// <remarks>
/// <b>No category row is touched.</b> A category points at its group by id, never by name, so the
/// breakdown reports the group under the new name on its own and the figure for every category in
/// it is unchanged (AC-28).
/// <para>
/// <b>Every check runs before the group is touched</b>, so a refused rename leaves the row exactly
/// as it was.
/// </para>
/// </remarks>
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
        // clash — AC-31 is about another group on that side already carrying the name.
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
