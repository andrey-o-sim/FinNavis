using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.Transactions;

public sealed class RemoveTransaction(ITransactionRepository transactions, IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(Guid transactionId, CancellationToken cancellationToken)
    {
        Transaction transaction = await transactions.GetByIdAsync(transactionId, cancellationToken)
            ?? throw TransactionErrors.NotFound(transactionId);

        transactions.Remove(transaction);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
