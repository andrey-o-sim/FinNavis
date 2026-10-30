using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Transfers;

public sealed class RecordTransfer(
    ITransferRepository transfers,
    IAccountRepository accounts,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Transfer> ExecuteAsync(
        Guid sourceAccountId,
        Guid destinationAccountId,
        decimal amount,
        DateOnly occurredOn,
        CancellationToken cancellationToken)
    {
        await RequireSourceAsync(sourceAccountId, cancellationToken);
        await RequireDestinationAsync(destinationAccountId, cancellationToken);

        // The entity refuses the same account on both sides and a non-positive amount, before
        // anything is staged.
        Transfer transfer = Transfer.Record(
            Guid.CreateVersion7(),
            sourceAccountId,
            destinationAccountId,
            new Money(amount),
            occurredOn,
            clock.UtcNow);

        transfers.Add(transfer);
        await TransferCommit.RunAsync(unitOfWork, cancellationToken);

        return transfer;
    }

    private async Task RequireSourceAsync(Guid accountId, CancellationToken cancellationToken) =>
        _ = await accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw TransferErrors.SourceAccountNotFound(accountId);

    private async Task RequireDestinationAsync(Guid accountId, CancellationToken cancellationToken) =>
        _ = await accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw TransferErrors.DestinationAccountNotFound(accountId);
}
