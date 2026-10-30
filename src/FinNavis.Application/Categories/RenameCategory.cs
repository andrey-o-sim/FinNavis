using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.Categories;

public sealed class RenameCategory(ICategoryRepository categories, IUnitOfWork unitOfWork)
{
    public async Task<Category> ExecuteAsync(
        Guid categoryId,
        string name,
        CancellationToken cancellationToken)
    {
        Category category = await categories.GetByIdAsync(categoryId, cancellationToken)
            ?? throw CategoryErrors.NotFound(categoryId);

        // The stored name is trimmed, so the comparison and the check have to use a trimmed name.
        string requestedName = (name ?? string.Empty).Trim();

        // The check answers true for the category's own row, so keeping its current name is not a
        // clash — the refusal is about another category on that side already carrying the name.
        bool nameChanges = !string.Equals(requestedName, category.Name, StringComparison.Ordinal);

        if (nameChanges
            && await categories.ExistsWithNameAsync(requestedName, category.Kind, cancellationToken))
        {
            throw CategoryErrors.DuplicateName(requestedName, category.Kind);
        }

        category.Rename(requestedName);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return category;
    }
}
