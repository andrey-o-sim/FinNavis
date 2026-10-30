using FinNavis.Application.Abstractions;
using FinNavis.Application.Common;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;

namespace FinNavis.Application.CategoryGroups;

/// <summary>
/// Creates a group on one named side of the ledger (AC-26, AC-31, sad.md §6 flow 13).
/// </summary>
/// <remarks>
/// <b>The new group holds no categories.</b> A category joins a group by pointing at it, so there
/// is nothing to write here to make the group empty — no category points at an id that was minted
/// a moment ago.
/// <para>
/// <b>The name check is not the guarantee — the unique index is.</b> The check is here so the
/// owner reads the refusal AC-31 words rather than a constraint violation; a race between two
/// writes still fails at <c>ux_category_groups_name_kind</c> instead of creating a duplicate
/// (sad.md §6 flow 13).
/// </para>
/// <para>
/// <b>The side is part of the check.</b> The index covers the name and the side together, so a
/// spending "Gifts" and an earning "Gifts" are two groups and neither blocks the other (AC-31).
/// </para>
/// </remarks>
public sealed class CreateCategoryGroup(
    ICategoryGroupRepository categoryGroups,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<CategoryGroup> ExecuteAsync(
        string name,
        CategoryKind kind,
        CancellationToken cancellationToken)
    {
        // Nothing is staged yet, so building the group first costs nothing and gives the check the
        // name the entity settled on — the stored name is trimmed.
        CategoryGroup group = CategoryGroup.Create(Guid.CreateVersion7(), name, kind, clock.UtcNow);

        if (await categoryGroups.ExistsWithNameAsync(group.Name, group.Kind, cancellationToken))
        {
            throw DuplicateName(group.Name, group.Kind);
        }

        categoryGroups.Add(group);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return group;
    }

    private static DomainRuleViolationException DuplicateName(string name, CategoryKind kind) =>
        new(
            "CategoryGroup.DuplicateName",
            $"An {kind.AsLedgerSide()} category group named '{name}' already exists.");
}
