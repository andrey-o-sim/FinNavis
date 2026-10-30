using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinNavis.Infrastructure.Repositories;

public sealed class CategoryRepository(FinNavisDbContext context) : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Categories.FirstOrDefaultAsync(category => category.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken) =>
        await context.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);

    // Tracked, unlike the plain list: deleting a group clears the link on every row this returns
    // and then commits them in the same unit of work.
    public async Task<IReadOnlyList<Category>> ListByGroupAsync(
        Guid groupId,
        CancellationToken cancellationToken) =>
        await context.Categories
            .Where(category => category.CategoryGroupId == groupId)
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);

    // Scoped exactly as ux_categories_name_kind is scoped: the name and the side together, so a
    // spending "Gifts" and an earning "Gifts" are two categories.
    public Task<bool> ExistsWithNameAsync(
        string name,
        CategoryKind kind,
        CancellationToken cancellationToken) =>
        context.Categories.AnyAsync(
            category => category.Name == name && category.Kind == kind,
            cancellationToken);

    public void Add(Category category) => context.Categories.Add(category);

    public void Remove(Category category) => context.Categories.Remove(category);
}
