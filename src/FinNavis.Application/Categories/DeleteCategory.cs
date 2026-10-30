using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.Categories;

/// <summary>
/// Removes a category no transaction names (AC-12, AC-25, sad.md §6 flow 6).
/// </summary>
/// <remarks>
/// <b>Recorded history blocks the removal rather than being deleted with it.</b> The foreign key
/// carries no cascade on purpose, so a transaction can never be orphaned or silently taken away
/// (<c>data-model.md</c>).
/// <para>
/// <b>The entity cannot answer this itself</b> — a category cannot see its own history, so the
/// question goes to <c>ILedgerQueries</c>, which asks whether a row exists and never loads one.
/// Only <c>transactions</c> is read: a transfer carries no category, so it can never name one.
/// </para>
/// <para>
/// <b>Removing a group is allowed where this is refused</b> (AC-29). A group is named by no
/// movement; a category is named directly by every transaction filed under it.
/// </para>
/// </remarks>
public sealed class DeleteCategory(
    ICategoryRepository categories,
    ILedgerQueries ledger,
    IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        Category category = await categories.GetByIdAsync(categoryId, cancellationToken)
            ?? throw CategoryErrors.NotFound(categoryId);

        if (await ledger.AnyTransactionNamesCategoryAsync(categoryId, cancellationToken))
        {
            throw CategoryErrors.HasRecordedHistory();
        }

        categories.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
