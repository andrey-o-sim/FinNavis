using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;

namespace FinNavis.Application.Categories;

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

        // The entity owns the two rules it can see on its own: the name bounds, and the refusal
        // when the group sits on the other side of the ledger.
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
