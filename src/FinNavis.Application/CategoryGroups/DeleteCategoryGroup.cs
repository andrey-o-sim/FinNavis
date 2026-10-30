using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Errors;

namespace FinNavis.Application.CategoryGroups;

/// <summary>
/// Removes a group and takes every category it held out of it (AC-29, AC-32).
/// </summary>
/// <remarks>
/// <b>Removing a group is allowed where removing an account (AC-09) or a category (AC-12) is
/// refused.</b> Those two are named directly by recorded movements; no movement names a group, so
/// nothing recorded can be orphaned by this (sad.md §5).
/// <para>
/// <b>No category is deleted and none loses a single entry.</b> Only the link is cleared, so every
/// category keeps its id, its name, its side and all of its recorded history (AC-29).
/// </para>
/// <para>
/// <b>Those categories are not moved into a group called Default.</b> There is no such row. The
/// breakdown produces that bucket at read time for categories with no group (sad.md §12,
/// <c>data-model.md</c> decision 8).
/// </para>
/// <para>
/// <b>Clearing the links and removing the group are one unit of work.</b> The foreign key is
/// <c>NO ACTION</c> on purpose, so a commit that reached the group row before the links would be
/// refused by PostgreSQL rather than leaving a category pointing at a group that is gone
/// (<c>data-model.md</c> decision 8).
/// </para>
/// </remarks>
public sealed class DeleteCategoryGroup(
    ICategoryGroupRepository categoryGroups,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(Guid categoryGroupId, CancellationToken cancellationToken)
    {
        CategoryGroup group = await categoryGroups.GetByIdAsync(categoryGroupId, cancellationToken)
            ?? throw NotFound(categoryGroupId);

        IReadOnlyList<Category> held = await categories.ListByGroupAsync(group.Id, cancellationToken);

        foreach (Category category in held)
        {
            category.ClearGroup();
        }

        categoryGroups.Remove(group);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static DomainRuleViolationException NotFound(Guid categoryGroupId) =>
        new("CategoryGroup.NotFound", $"No category group in the ledger has id {categoryGroupId}.");
}
