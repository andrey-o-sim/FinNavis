using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;

namespace FinNavis.Domain.Abstractions;

public interface ICategoryGroupRepository
{
    Task<CategoryGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<CategoryGroup>> ListAsync(CancellationToken cancellationToken);

    Task<bool> ExistsWithNameAsync(string name, CategoryKind kind, CancellationToken cancellationToken);

    void Add(CategoryGroup group);

    void Remove(CategoryGroup group);
}
