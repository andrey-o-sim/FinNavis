using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Transactions;

public sealed class RecordTransaction(
    ITransactionRepository transactions,
    IAccountRepository accounts,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Transaction> ExecuteAsync(
        Guid accountId,
        Guid categoryId,
        decimal amount,
        DateOnly occurredOn,
        string? note,
        CancellationToken cancellationToken)
    {
        await RequireAccountAsync(accountId, cancellationToken);
        await RequireCategoryAsync(categoryId, cancellationToken);

        Transaction transaction = Transaction.Record(
            Guid.CreateVersion7(),
            accountId,
            categoryId,
            new Money(amount),
            occurredOn,
            note,
            clock.UtcNow);

        transactions.Add(transaction);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return transaction;
    }

    private async Task RequireAccountAsync(Guid accountId, CancellationToken cancellationToken) =>
        _ = await accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw TransactionErrors.AccountNotFound(accountId);

    private async Task RequireCategoryAsync(Guid categoryId, CancellationToken cancellationToken) =>
        _ = await categories.GetByIdAsync(categoryId, cancellationToken)
            ?? throw TransactionErrors.CategoryNotFound(categoryId);
}
