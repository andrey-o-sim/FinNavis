using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Transactions;

public sealed class EditTransaction(
    ITransactionRepository transactions,
    IAccountRepository accounts,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork)
{
    public async Task<Transaction> ExecuteAsync(
        Guid transactionId,
        EditTransactionChanges changes,
        CancellationToken cancellationToken)
    {
        Transaction transaction = await transactions.GetByIdAsync(transactionId, cancellationToken)
            ?? throw TransactionErrors.NotFound(transactionId);

        if (changes.AccountId is Guid accountId)
        {
            _ = await accounts.GetByIdAsync(accountId, cancellationToken)
                ?? throw TransactionErrors.AccountNotFound(accountId);
        }

        if (changes.CategoryId is Guid categoryId)
        {
            _ = await categories.GetByIdAsync(categoryId, cancellationToken)
                ?? throw TransactionErrors.CategoryNotFound(categoryId);
        }

        // The entity takes the whole entry, so every field it is not changing is passed back as it
        // stands. It refuses the second account, the non-positive amount and the over-long note
        // before it assigns the first field.
        transaction.Edit(
            changes.AccountId ?? transaction.AccountId,
            changes.SecondAccountId,
            changes.CategoryId ?? transaction.CategoryId,
            changes.Amount is decimal amount ? new Money(amount) : transaction.Amount,
            changes.OccurredOn ?? transaction.OccurredOn,
            changes.Note.Or(transaction.Note));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return transaction;
    }
}
