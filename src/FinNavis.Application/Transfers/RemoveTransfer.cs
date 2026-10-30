using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.Transfers;

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
