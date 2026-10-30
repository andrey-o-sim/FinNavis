using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.Transfers;

/// <summary>
/// Removes a recorded transfer (AC-22, AC-25, ADR-0003).
/// </summary>
/// <remarks>
/// <b>A hard delete.</b> The transfer disappears from history — there is no reversing entry, no
/// <c>deleted_at</c> and no audit table.
/// <para>
/// <b>Both balances return to what they would have been, and the owner's total is unchanged
/// either way</b> (AC-15). One row carried both sides, so removing it takes the same amount off
/// one account and puts it back on the other, and neither figure is stored to begin with
/// (ADR-0002).
/// </para>
/// </remarks>
public sealed class RemoveTransfer(ITransferRepository transfers, IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(Guid transferId, CancellationToken cancellationToken)
    {
        Transfer transfer = await transfers.GetByIdAsync(transferId, cancellationToken)
            ?? throw TransferErrors.NotFound(transferId);

        transfers.Remove(transfer);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
