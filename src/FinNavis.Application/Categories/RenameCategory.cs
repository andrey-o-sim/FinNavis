using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.Categories;

/// <summary>
/// Gives a category a new name and keeps its identity, its side and its group (AC-34, AC-36).
/// </summary>
/// <remarks>
/// <b>Nothing is rewritten.</b> Every transaction names its category by id, never by name, so the
/// history, the balances and the breakdown follow the new name on their own. That is also why a
/// closed period's figure cannot move: the rename touches no transaction row and changes no
/// amount (AC-34).
/// <para>
/// <b>Every check runs before the category is touched</b>, so a refused rename leaves the row
/// exactly as it was.
/// </para>
/// </remarks>
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
        // clash — AC-36 is about another category on that side already carrying the name.
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
