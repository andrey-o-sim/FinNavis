using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.Categories;

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

            // The entity owns the rule: a group and the categories inside it are on one side.
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
