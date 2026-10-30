using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.CategoryGroups;

/// <summary>
/// Reads every group with its name and its side of the ledger (AC-27).
/// </summary>
/// <remarks>
/// <b>A group holding no categories is returned like any other.</b> The list comes from
/// <c>category_groups</c> alone and never joins <c>categories</c>, so an empty group cannot fall
/// out of it.
/// <para>
/// <b>There is no paging.</b> The table holds a handful of rows — the owner writes them by hand
/// (<c>data-model.md</c>, access patterns).
/// </para>
/// </remarks>
public sealed class ListCategoryGroups(ICategoryGroupRepository categoryGroups)
{
    public Task<IReadOnlyList<CategoryGroup>> ExecuteAsync(CancellationToken cancellationToken) =>
        categoryGroups.ListAsync(cancellationToken);
}
