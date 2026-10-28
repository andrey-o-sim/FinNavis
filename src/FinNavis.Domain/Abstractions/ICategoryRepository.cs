using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;

namespace FinNavis.Domain.Abstractions;

/// <summary>
/// A collection-like view of the <see cref="Category"/> aggregate. A repository port: the other
/// side of it is the data store, and Infrastructure supplies the adapter (sad.md §5).
/// </summary>
/// <remarks>
/// Write path only. Balances, history and the breakdown never load an entity — they go through
/// <c>ILedgerQueries</c> in the Application layer (ADR-0004).
/// </remarks>
public interface ICategoryRepository
{
    /// <summary>Loads one category, or <c>null</c> when no category carries that id (AC-25).</summary>
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Loads every category with its group link, used and unused alike (AC-33). The table holds a
    /// handful of rows, so there is no paging (<c>data-model.md</c>, access patterns).
    /// </summary>
    Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Loads the categories sitting in one group. Serves both the narrowed list (AC-33) and the
    /// categories whose link has to be cleared before the group is removed (AC-29).
    /// </summary>
    /// <remarks>
    /// Reads <c>categories</c> by group, which is what <c>idx_categories_category_group_id</c> is
    /// there for (<c>data-model.md</c>, access patterns).
    /// </remarks>
    Task<IReadOnlyList<Category>> ListByGroupAsync(Guid groupId, CancellationToken cancellationToken);

    /// <summary>
    /// Answers whether a category on this side of the ledger already carries this name (AC-36,
    /// sad.md §6 flow 13).
    /// </summary>
    /// <remarks>
    /// Scoped exactly as <c>ux_categories_name_kind</c> is scoped — the name and the side
    /// together, so a spending "Gifts" and an earning "Gifts" are two categories. A rename to the
    /// category's own current name therefore answers <c>true</c>; the use case decides what that
    /// means.
    /// </remarks>
    Task<bool> ExistsWithNameAsync(string name, CategoryKind kind, CancellationToken cancellationToken);

    /// <summary>
    /// Stages a new category. Nothing reaches PostgreSQL until the unit of work commits.
    /// </summary>
    void Add(Category category);

    /// <summary>
    /// Stages the removal of a category (AC-12). The caller has already checked that no
    /// transaction names it — the entity cannot see its own history, and the foreign key carries
    /// no cascade on purpose (<c>data-model.md</c>).
    /// </summary>
    void Remove(Category category);
}
