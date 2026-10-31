namespace FinNavis.Domain.Entities;

public sealed class ImportCreatedAccountLink
{
    private ImportCreatedAccountLink(
        Guid id,
        Guid importRunId,
        Guid accountId,
        DateTimeOffset createdAt)
    {
        Id = id;
        ImportRunId = importRunId;
        AccountId = accountId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid ImportRunId { get; private set; }

    public Guid AccountId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    internal static ImportCreatedAccountLink For(
        Guid id,
        Guid importRunId,
        Account account,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(account);

        return new ImportCreatedAccountLink(id, importRunId, account.Id, createdAt);
    }
}
