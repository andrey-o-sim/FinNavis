using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;

namespace FinNavis.Domain.Abstractions;

/// <summary>
/// A collection-like view of the <see cref="CategoryGroup"/> aggregate. A repository port: the
/// other side of it is the data store, and Infrastructure supplies the adapter (sad.md §5).
/// </summary>
/// <remarks>
/// It exists apart from <c>ICategoryRepository</c> because US-09 makes the group lifecycle —
/// create, list, rename, remove — a job of its own (sad.md §5, <c>data-model.md</c>).
/// <para>
/// Write path only. The breakdown never loads a group: it resolves the category-to-group mapping
/// through <c>ILedgerQueries</c> (ADR-0004).
/// </para>
/// </remarks>
public interface ICategoryGroupRepository
{
    /// <summary>Loads one group, or <c>null</c> when no group carries that id (AC-25, AC-32).</summary>
    Task<CategoryGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Loads every group, those holding no categories included (AC-27). The table holds a handful
    /// of rows, so there is no paging (<c>data-model.md</c>, access patterns).
    /// </summary>
    Task<IReadOnlyList<CategoryGroup>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Answers whether a group on this side of the ledger already carries this name (AC-31,
    /// sad.md §6 flow 13).
    /// </summary>
    /// <remarks>
    /// Scoped exactly as <c>ux_category_groups_name_kind</c> is scoped — the name and the side
    /// together, so a spending "Gifts" and an earning "Gifts" are two groups. A rename to the
    /// group's own current name therefore answers <c>true</c>; the use case decides what that
    /// means.
    /// </remarks>
    Task<bool> ExistsWithNameAsync(string name, CategoryKind kind, CancellationToken cancellationToken);

    /// <summary>
    /// Stages a new group. Nothing reaches PostgreSQL until the unit of work commits.
    /// </summary>
    void Add(CategoryGroup group);

    /// <summary>
    /// Stages the removal of a group (AC-29). The caller has already cleared the link on every
    /// category that pointed at it, in the same unit of work — the foreign key is
    /// <c>NO ACTION</c> on purpose, so forgetting that step fails loudly
    /// (<c>data-model.md</c> decision 8).
    /// </summary>
    void Remove(CategoryGroup group);
}
