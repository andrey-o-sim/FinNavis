using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Transfers;

/// <summary>
/// Records a move of money between two of the owner's own accounts (AC-04, AC-05, AC-06, AC-25,
/// sad.md §6 flow 2).
/// </summary>
/// <remarks>
/// <b>The source falls and the destination rises by the same amount, and the owner's total is
/// unchanged.</b> That holds by construction, not by a check: one row carries both sides and one
/// amount, so the two halves cannot disagree and no sum over the accounts can drift (AC-04,
/// AC-15).
/// <para>
/// <b>A half-applied transfer is not representable</b> (<c>data-model.md</c>). AC-06 is therefore
/// stronger than a rollback guarantee — there is no shape of the data in which one account moved
/// and the other did not. A commit that fails leaves both accounts as they were, and the owner
/// reads that in the refusal.
/// </para>
/// <para>
/// <b>Which account was not found is part of the refusal.</b> One request names the same aggregate
/// twice, so the owner has to know which of the two to correct (<c>docs/adr/0004</c>).
/// </para>
/// </remarks>
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

        // The entity owns AC-05 and the positive amount. Both run before anything is staged.
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
