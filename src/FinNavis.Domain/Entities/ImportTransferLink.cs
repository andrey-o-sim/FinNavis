using FinNavis.Domain.ValueObjects;

namespace FinNavis.Domain.Entities;

public sealed class ImportTransferLink
{
    private ImportTransferLink(
        Guid id,
        Guid importRunId,
        Guid transferId,
        string fingerprint,
        DateTimeOffset createdAt)
    {
        Id = id;
        ImportRunId = importRunId;
        TransferId = transferId;
        Fingerprint = fingerprint;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid ImportRunId { get; private set; }

    public Guid TransferId { get; private set; }

    public string Fingerprint { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public bool StillMatches(Transfer transfer)
    {
        ArgumentNullException.ThrowIfNull(transfer);

        if (transfer.Id != TransferId)
        {
            throw new ArgumentException(
                $"The link describes transfer {TransferId}, not {transfer.Id}.",
                nameof(transfer));
        }

        return Fingerprint == FingerprintOf(transfer);
    }

    internal static ImportTransferLink For(
        Guid id,
        Guid importRunId,
        Transfer transfer,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(transfer);

        return new ImportTransferLink(
            id, importRunId, transfer.Id, FingerprintOf(transfer), createdAt);
    }

    private static string FingerprintOf(Transfer transfer) =>
        MovementFingerprint.OfTransfer(
            transfer.SourceAccountId,
            transfer.DestinationAccountId,
            transfer.Amount,
            transfer.OccurredOn).Value;
}
