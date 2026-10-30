using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Transfers;

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
