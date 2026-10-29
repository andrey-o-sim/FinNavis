using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinNavis.Infrastructure.Repositories;

/// <summary>
/// The EF Core adapter behind <see cref="ICategoryGroupRepository"/> (sad.md §5).
/// </summary>
/// <remarks>
/// Write path only. Nothing here reaches PostgreSQL until <c>IUnitOfWork</c> commits.
/// </remarks>
public sealed class CategoryGroupRepository(FinNavisDbContext context) : ICategoryGroupRepository
{
    public Task<CategoryGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.CategoryGroups.FirstOrDefaultAsync(group => group.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CategoryGroup>> ListAsync(CancellationToken cancellationToken) =>
        await context.CategoryGroups
            .AsNoTracking()
            .OrderBy(group => group.Name)
            .ToListAsync(cancellationToken);

    // Scoped exactly as ux_category_groups_name_kind is scoped: the name and the side together,
    // so a spending "Gifts" and an earning "Gifts" are two groups (AC-31).
    public Task<bool> ExistsWithNameAsync(
        string name,
        CategoryKind kind,
        CancellationToken cancellationToken) =>
        context.CategoryGroups.AnyAsync(
            group => group.Name == name && group.Kind == kind,
            cancellationToken);

    public void Add(CategoryGroup group) => context.CategoryGroups.Add(group);

    public void Remove(CategoryGroup group) => context.CategoryGroups.Remove(group);
}
