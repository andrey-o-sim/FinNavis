using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Domain.Entities;

public sealed class Transaction
{
    public const int MaxNoteLength = 500;

    public const int MaxExternalRefLength = 200;

    private Transaction(
        Guid id,
        Guid accountId,
        Guid categoryId,
        Money amount,
        DateOnly occurredOn,
        string? note,
        DateTimeOffset createdAt)
    {
        Id = id;
        AccountId = accountId;
        CategoryId = categoryId;
        Amount = amount;
        OccurredOn = occurredOn;
        Note = note;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public Guid CategoryId { get; private set; }

    public Money Amount { get; private set; }

    public DateOnly OccurredOn { get; private set; }

    public string? Note { get; private set; }

    public string? ExternalRef { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Transaction Record(
        Guid id,
        Guid accountId,
        Guid categoryId,
        Money amount,
        DateOnly occurredOn,
        string? note,
        DateTimeOffset createdAt)
    {
        RequirePositiveAmount(amount);
        RequireNoteWithinBound(note);

        return new Transaction(id, accountId, categoryId, amount, occurredOn, note, createdAt);
    }

    public void Edit(
        Guid accountId,
        Guid? secondAccountId,
        Guid categoryId,
        Money amount,
        DateOnly occurredOn,
        string? note)
    {
        if (secondAccountId is not null)
        {
            throw new DomainRuleViolationException(
                "Transaction.KindChangeNotAllowed",
                "A transaction and a transfer are different kinds of entry. "
                    + "Remove this entry and record it again as a transfer.");
        }

        RequirePositiveAmount(amount);
        RequireNoteWithinBound(note);

        AccountId = accountId;
        CategoryId = categoryId;
        Amount = amount;
        OccurredOn = occurredOn;
        Note = note;
    }

    private static void RequirePositiveAmount(Money amount)
    {
        if (!amount.IsPositive)
        {
            throw new DomainRuleViolationException(
                "Transaction.AmountNotPositive",
                "The amount must be above zero.");
        }
    }

    private static void RequireNoteWithinBound(string? note)
    {
        if (note is not null && note.Length > MaxNoteLength)
        {
            throw new DomainRuleViolationException(
                "Transaction.NoteTooLong",
                "A note may be at most 500 characters.");
        }
    }
}
