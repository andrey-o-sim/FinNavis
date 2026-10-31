using FinNavis.Domain.ValueObjects;

namespace FinNavis.Domain.Entities;

public sealed class ImportTransactionLink
{
    private ImportTransactionLink(
        Guid id,
        Guid importRunId,
        Guid transactionId,
        string fingerprint,
        DateTimeOffset createdAt)
    {
        Id = id;
        ImportRunId = importRunId;
        TransactionId = transactionId;
        Fingerprint = fingerprint;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid ImportRunId { get; private set; }

    public Guid TransactionId { get; private set; }

    public string Fingerprint { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public bool StillMatches(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (transaction.Id != TransactionId)
        {
            throw new ArgumentException(
                $"The link describes transaction {TransactionId}, not {transaction.Id}.",
                nameof(transaction));
        }

        return Fingerprint == FingerprintOf(transaction);
    }

    internal static ImportTransactionLink For(
        Guid id,
        Guid importRunId,
        Transaction transaction,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        return new ImportTransactionLink(
            id, importRunId, transaction.Id, FingerprintOf(transaction), createdAt);
    }

    private static string FingerprintOf(Transaction transaction) =>
        MovementFingerprint.OfTransaction(
            transaction.AccountId,
            transaction.CategoryId,
            transaction.Amount,
            transaction.OccurredOn,
            transaction.Note).Value;
}
