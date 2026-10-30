using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Transactions;

/// <summary>
/// Applies the owner's correction to a recorded entry, in place (AC-21, AC-24, AC-25,
/// sad.md §6 flows 4 and 9).
/// </summary>
/// <remarks>
/// <b>There is no reversing entry</b> (ADR-0003). The row itself changes, and every balance the
/// entry affects follows on the next read, because no balance is stored (ADR-0002). Nothing has to
/// be corrected afterwards.
/// <para>
/// <b>A transaction can never become a transfer</b> (AC-24). The HTTP contract has no
/// second-account property and refuses an unknown one structurally; the entity refuses it as well,
/// so the rule holds whichever surface the request came through.
/// </para>
/// <para>
/// <b>An account or category the entry moves onto is looked up first</b>, so a refused edit leaves
/// the entry exactly as it was. A field the owner left out is not looked up at all — it already
/// points at a row the ledger holds.
/// </para>
/// </remarks>
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
