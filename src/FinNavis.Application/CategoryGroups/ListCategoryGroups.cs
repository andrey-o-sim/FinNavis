using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.CategoryGroups;

public sealed class ListCategoryGroups(ICategoryGroupRepository categoryGroups)
{
    public Task<IReadOnlyList<CategoryGroup>> ExecuteAsync(CancellationToken cancellationToken) =>
        categoryGroups.ListAsync(cancellationToken);
}
