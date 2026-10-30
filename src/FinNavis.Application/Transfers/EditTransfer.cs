using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Transfers;

/// <summary>
/// Applies the owner's correction to a recorded transfer, in place (AC-21, AC-23, AC-25,
/// sad.md §6 flow 4).
/// </summary>
/// <remarks>
/// <b>A transfer carries no category</b> (AC-23). The entity has no field for one, so an attempt
/// to give it a category is refused here with the sentence the owner reads — it cannot be
/// silently dropped on the way in.
/// <para>
/// <b>There is no reversing entry</b> (ADR-0003). The row itself changes, and both balances follow
/// on the next read because no balance is stored (ADR-0002).
/// </para>
/// <para>
/// <b>Every check runs before the first field is assigned</b>, so a refused edit leaves the
/// transfer exactly as it was.
/// </para>
/// </remarks>
public sealed class EditTransfer(
    ITransferRepository transfers,
    IAccountRepository accounts,
    IUnitOfWork unitOfWork)
{
    public async Task<Transfer> ExecuteAsync(
        Guid transferId,
        EditTransferChanges changes,
        CancellationToken cancellationToken)
    {
        if (changes.CategoryId is not null)
        {
            throw TransferErrors.CategoryNotAllowed();
        }

        Transfer transfer = await transfers.GetByIdAsync(transferId, cancellationToken)
            ?? throw TransferErrors.NotFound(transferId);

        if (changes.SourceAccountId is Guid sourceAccountId)
        {
            _ = await accounts.GetByIdAsync(sourceAccountId, cancellationToken)
                ?? throw TransferErrors.SourceAccountNotFound(sourceAccountId);
        }

        if (changes.DestinationAccountId is Guid destinationAccountId)
        {
            _ = await accounts.GetByIdAsync(destinationAccountId, cancellationToken)
                ?? throw TransferErrors.DestinationAccountNotFound(destinationAccountId);
        }

        // The entity takes the whole transfer, so every field the edit is not changing is passed
        // back as it stands. It refuses the same account on both sides and a non-positive amount
        // before it assigns the first field.
        transfer.Edit(
            changes.SourceAccountId ?? transfer.SourceAccountId,
            changes.DestinationAccountId ?? transfer.DestinationAccountId,
            changes.Amount is decimal amount ? new Money(amount) : transfer.Amount,
            changes.OccurredOn ?? transfer.OccurredOn);

        await TransferCommit.RunAsync(unitOfWork, cancellationToken);

        return transfer;
    }
}
