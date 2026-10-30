using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.Transactions;

/// <summary>
/// Removes a recorded entry (AC-22, AC-25, ADR-0003).
/// </summary>
/// <remarks>
/// <b>A hard delete.</b> The entry disappears from history — there is no reversing entry, no
/// <c>deleted_at</c> and no audit table. A wrong delete is unrecoverable outside the nightly
/// <c>pg_dump</c> (sad.md §7, §11).
/// <para>
/// <b>Every balance it affected returns to what it would have been, and that costs nothing.</b>
/// No balance is stored, so each one is summed from the movements that are left on the next read
/// (ADR-0002).
/// </para>
/// <para>
/// <b>Nothing blocks this.</b> An account and a category are refused while history names them
/// (AC-09, AC-12), because something would be orphaned; an entry *is* the history, so removing it
/// orphans nothing.
/// </para>
/// </remarks>
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
