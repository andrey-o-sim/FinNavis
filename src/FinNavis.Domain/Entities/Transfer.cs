using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Domain.Entities;

public sealed class Transfer
{
    private Transfer(
        Guid id,
        Guid sourceAccountId,
        Guid destinationAccountId,
        Money amount,
        DateOnly occurredOn,
        DateTimeOffset createdAt)
    {
        Id = id;
        SourceAccountId = sourceAccountId;
        DestinationAccountId = destinationAccountId;
        Amount = amount;
        OccurredOn = occurredOn;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid SourceAccountId { get; private set; }

    public Guid DestinationAccountId { get; private set; }

    public Money Amount { get; private set; }

    public DateOnly OccurredOn { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Transfer Record(
        Guid id,
        Guid sourceAccountId,
        Guid destinationAccountId,
        Money amount,
        DateOnly occurredOn,
        DateTimeOffset createdAt)
    {
        RequireTwoDifferentAccounts(sourceAccountId, destinationAccountId);
        RequirePositiveAmount(amount);
        RequireAmountTheLedgerCanHold(amount);

        return new Transfer(
            id, sourceAccountId, destinationAccountId, amount, occurredOn, createdAt);
    }

    public void Edit(
        Guid sourceAccountId,
        Guid destinationAccountId,
        Money amount,
        DateOnly occurredOn)
    {
        RequireTwoDifferentAccounts(sourceAccountId, destinationAccountId);
        RequirePositiveAmount(amount);
        RequireAmountTheLedgerCanHold(amount);

        SourceAccountId = sourceAccountId;
        DestinationAccountId = destinationAccountId;
        Amount = amount;
        OccurredOn = occurredOn;
    }

    private static void RequireTwoDifferentAccounts(
        Guid sourceAccountId,
        Guid destinationAccountId)
    {
        if (sourceAccountId == destinationAccountId)
        {
            throw new DomainRuleViolationException(
                "Transfer.SameAccount",
                "A transfer needs two different accounts.");
        }
    }

    private static void RequirePositiveAmount(Money amount)
    {
        if (!amount.IsPositive)
        {
            throw new DomainRuleViolationException(
                "Transfer.AmountNotPositive",
                "The amount must be above zero.");
        }
    }

    // Refused here rather than left to the database, where it arrives as a write failure the
    // owner cannot act on.
    private static void RequireAmountTheLedgerCanHold(Money amount)
    {
        if (!amount.FitsLedgerColumn)
        {
            throw new DomainRuleViolationException(
                "Transfer.AmountOutOfRange",
                "The amount must not be above 999999999999999.9999.");
        }
    }
}
