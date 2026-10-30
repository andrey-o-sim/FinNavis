using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.Categories;

/// <summary>
/// Reads every category, or only those sitting in one named group (AC-33, AC-32).
/// </summary>
/// <remarks>
/// <b>Each category carries its own group link.</b> Nothing is resolved or joined here — the link
/// is the group's id, and Presentation reads the group names it needs from
/// <c>ListCategoryGroups</c>.
/// <para>
/// <b>A category with no group is returned with an empty link.</b> It is not reported as sitting
/// in a group called Default; that bucket belongs to the breakdown and is never stored
/// (sad.md §12).
/// </para>
/// <para>
/// <b>An unknown group is refused rather than answered with an empty list</b> (AC-32). An empty
/// list would say "this group holds nothing", which is a different fact from "there is no such
/// group".
/// </para>
/// </remarks>
public sealed class ListCategories(
    ICategoryRepository categories,
    ICategoryGroupRepository categoryGroups)
{
    public async Task<IReadOnlyList<Category>> ExecuteAsync(
        Guid? categoryGroupId,
        CancellationToken cancellationToken)
    {
        if (categoryGroupId is not Guid groupId)
        {
            return await categories.ListAsync(cancellationToken);
        }

        _ = await categoryGroups.GetByIdAsync(groupId, cancellationToken)
            ?? throw CategoryErrors.GroupNotFound(groupId);

        return await categories.ListByGroupAsync(groupId, cancellationToken);
    }
}
