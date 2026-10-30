using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;

namespace FinNavis.Application.Categories;

/// <summary>
/// Creates a category on one side of the ledger, with a group or without one (AC-11, AC-30, AC-32,
/// AC-36, sad.md §6 flow 13).
/// </summary>
/// <remarks>
/// <b>An unknown group is named back and never created.</b> AC-32 is explicit: the change is
/// blocked and nothing is written, not the category and not the group.
/// <para>
/// <b>No group is the same as no group.</b> A category created without one carries a null link and
/// joins nothing. The breakdown reports it under the Default group, which it produces at read time
/// and never stores (sad.md §12).
/// </para>
/// <para>
/// <b>The name check is not the guarantee — the unique index is.</b> The check is here so the
/// owner reads the refusal AC-36 words rather than a constraint violation; a race between two
/// writes still fails at <c>ux_categories_name_kind</c> instead of creating a duplicate.
/// </para>
/// </remarks>
public sealed class CreateCategory(
    ICategoryRepository categories,
    ICategoryGroupRepository categoryGroups,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Category> ExecuteAsync(
        string name,
        CategoryKind kind,
        Guid? categoryGroupId,
        CancellationToken cancellationToken)
    {
        CategoryGroup? group = await LoadGroupAsync(categoryGroupId, cancellationToken);

        // The entity owns the two rules it can see on its own: the name bounds, and AC-30's
        // refusal when the group sits on the other side of the ledger.
        Category category = Category.Create(
            Guid.CreateVersion7(),
            name,
            kind,
            group,
            clock.UtcNow);

        if (await categories.ExistsWithNameAsync(category.Name, category.Kind, cancellationToken))
        {
            throw CategoryErrors.DuplicateName(category.Name, category.Kind);
        }

        categories.Add(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return category;
    }

    private async Task<CategoryGroup?> LoadGroupAsync(
        Guid? categoryGroupId,
        CancellationToken cancellationToken)
    {
        if (categoryGroupId is not Guid groupId)
        {
            return null;
        }

        return await categoryGroups.GetByIdAsync(groupId, cancellationToken)
            ?? throw CategoryErrors.GroupNotFound(groupId);
    }
}
