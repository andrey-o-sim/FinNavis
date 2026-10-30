using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.Categories;

public sealed class ListCategories(
    ICategoryRepository categories,
    ICategoryGroupRepository categoryGroups)
{
    public async Task<IReadOnlyList<Category>> ExecuteAsync(
        Guid? categoryGroupId,
        CancellationToken cancellationToken)
    {
        if (categoryGroupId is not Guid groupId)
        {
            return await categories.ListAsync(cancellationToken);
        }

        _ = await categoryGroups.GetByIdAsync(groupId, cancellationToken)
            ?? throw CategoryErrors.GroupNotFound(groupId);

        return await categories.ListByGroupAsync(groupId, cancellationToken);
    }
}
