using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Abstractions.Reads;

/// <summary>
/// One category's total for a period, with the group the category sits in right now. The raw row
/// the breakdown is assembled from.
/// </summary>
/// <param name="CategoryId">The category the total belongs to.</param>
/// <param name="CategoryName">The category's name, joined in so a caller needs no second read.</param>
/// <param name="Kind">
/// Which side of the ledger the total belongs to. Spending and earning are kept apart and never
/// added together (AC-18).
/// </param>
/// <param name="CategoryGroupId">
/// The group the category belongs to right now, or <c>null</c> when it belongs to none.
/// </param>
/// <param name="CategoryGroupName">
/// That group's name, or <c>null</c> alongside a <c>null</c> <paramref name="CategoryGroupId"/>.
/// </param>
/// <param name="Total">The sum of the category's transactions in the period. Always above zero.</param>
/// <remarks>
/// <b>A <c>null</c> group is not the Default group row — there is no such row.</b> The Default
/// group is a bucket the breakdown use case produces for every category belonging to no group
/// (sad.md §12, <c>data-model.md</c> decision 8). Nothing reads or writes it, and no category is
/// ever assigned to it.
/// <para>
/// <b>The group total is not a column here.</b> It is the sum of the rows sharing a
/// <paramref name="CategoryGroupId"/>, added up by the use case in the same pass that builds the
/// Default bucket. Doing it in one place is what makes the group figures and the Default figure
/// agree (AC-19).
/// </para>
/// <para>
/// <b>The mapping is resolved at query time</b> (ADR-0002), so moving a category between groups
/// changes group figures at once and can never change that category's own figure for a closed
/// period (AC-13).
/// </para>
/// <para>
/// <b>No transfer is ever counted here</b> (AC-20). The query reads <c>transactions</c> and never
/// touches <c>transfers</c>, so this holds structurally rather than by a filter someone could
/// forget (ADR-0001).
/// </para>
/// </remarks>
public sealed record BreakdownCategoryRow(
    Guid CategoryId,
    string CategoryName,
    CategoryKind Kind,
    Guid? CategoryGroupId,
    string? CategoryGroupName,
    Money Total);
