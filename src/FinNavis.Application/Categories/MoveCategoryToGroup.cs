using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.Categories;

/// <summary>
/// Puts a category in a group, moves it between groups, or takes it out of every group (AC-13,
/// AC-30, AC-32, sad.md §6 flow 12).
/// </summary>
/// <remarks>
/// <b>One row changes: this category's group link.</b> No transaction is touched, so every entry
/// already filed under the category keeps its amount and its date, and a closed period's figure is
/// unchanged. What moves is where the breakdown rolls the category up from the next read on
/// (AC-13).
/// <para>
/// <b>Taking it out of every group is the same operation with no group named.</b> The link is
/// cleared and nothing is assigned — the category does not join a group called Default, because
/// there is no such row (sad.md §12).
/// </para>
/// </remarks>
public sealed class MoveCategoryToGroup(
    ICategoryRepository categories,
    ICategoryGroupRepository categoryGroups,
    IUnitOfWork unitOfWork)
{
    public async Task<Category> ExecuteAsync(
        Guid categoryId,
        Guid? categoryGroupId,
        CancellationToken cancellationToken)
    {
        Category category = await categories.GetByIdAsync(categoryId, cancellationToken)
            ?? throw CategoryErrors.NotFound(categoryId);

        if (categoryGroupId is Guid groupId)
        {
            CategoryGroup group = await categoryGroups.GetByIdAsync(groupId, cancellationToken)
                ?? throw CategoryErrors.GroupNotFound(groupId);

            // AC-30 is the entity's rule: a group and the categories inside it are on one side.
            category.PlaceInGroup(group);
        }
        else
        {
            category.ClearGroup();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return category;
    }
}
