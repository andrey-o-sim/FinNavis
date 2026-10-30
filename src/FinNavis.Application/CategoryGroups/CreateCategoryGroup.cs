using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;

namespace FinNavis.Application.CategoryGroups;

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

        CategoryGroupName.RequireNotReserved(group.Name);

        if (await categoryGroups.ExistsWithNameAsync(group.Name, group.Kind, cancellationToken))
        {
            throw CategoryGroupErrors.DuplicateName(group.Name, group.Kind);
        }

        categoryGroups.Add(group);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return group;
    }
}
