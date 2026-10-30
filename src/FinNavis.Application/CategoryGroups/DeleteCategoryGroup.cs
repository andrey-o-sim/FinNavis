using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.CategoryGroups;

public sealed class DeleteCategoryGroup(
    ICategoryGroupRepository categoryGroups,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(Guid categoryGroupId, CancellationToken cancellationToken)
    {
        CategoryGroup group = await categoryGroups.GetByIdAsync(categoryGroupId, cancellationToken)
            ?? throw CategoryGroupErrors.NotFound(categoryGroupId);

        IReadOnlyList<Category> held = await categories.ListByGroupAsync(group.Id, cancellationToken);

        foreach (Category category in held)
        {
            category.ClearGroup();
        }

        categoryGroups.Remove(group);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
