using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.Categories;

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
