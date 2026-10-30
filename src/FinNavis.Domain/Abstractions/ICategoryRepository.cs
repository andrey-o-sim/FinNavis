using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;

namespace FinNavis.Domain.Abstractions;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Category>> ListByGroupAsync(Guid groupId, CancellationToken cancellationToken);

    Task<bool> ExistsWithNameAsync(string name, CategoryKind kind, CancellationToken cancellationToken);

    void Add(Category category);

    void Remove(Category category);
}
