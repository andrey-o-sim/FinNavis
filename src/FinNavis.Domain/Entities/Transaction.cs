using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Domain.Entities;

/// <summary>
/// One movement of money on a single account, filed under a single category — a purchase, a
/// salary. An aggregate root.
/// </summary>
/// <remarks>
/// <b>The amount is always above zero</b> (AC-02). Direction is not stored: a balance joins the
/// transaction to its category to learn whether the row adds or subtracts (<c>data-model.md</c>
/// decision 3). A stored negative would contradict the acceptance criterion the amount is meant to
/// satisfy.
/// <para>
/// <b>Exactly one account.</b> A movement that touched two accounts is a <c>Transfer</c>, and the
/// two are separate entities with separate tables (ADR-0001). Neither can become the other by
/// editing — see <see cref="Edit"/> and AC-24.
/// </para>
/// <para>
/// <b>Rows are edited and deleted in place</b> (ADR-0003). There is no reversing entry, no
/// <c>deleted_at</c> and no audit table: AC-22 says a removed entry disappears from history, which
/// is a hard delete.
/// </para>
/// <para>
/// <b>The category is named, the group is not.</b> There is no group field here. A rename or a move
/// of the category rewrites no movement row, which is what makes AC-34 and AC-13 free
/// (<c>data-model.md</c>).
/// </para>
/// <para>
/// <b>AC-03 (an unknown category) is not here.</b> The entity cannot look up a category. That check
/// is in the Application layer (sad.md §6 flow 1).
/// </para>
/// </remarks>
public sealed class Transaction
{
    /// <summary>
    /// The longest note the ledger stores. Matches <c>transactions.note VARCHAR(500)</c>
    /// (<c>data-model.md</c> decision 2), which also closes the storage-bloat abuse case in
    /// PRD §6.1.
    /// </summary>
    public const int MaxNoteLength = 500;

    /// <summary>
    /// The longest outside reference the ledger stores. Matches
    /// <c>transactions.external_ref VARCHAR(200)</c> (<c>data-model.md</c>). Unused in this
    /// feature — see <see cref="ExternalRef"/>.
    /// </summary>
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

    /// <summary>
    /// The identity of the entry. A UUID v7 minted in the Application layer (ADR-0005).
    /// </summary>
    /// <remarks>
    /// The value is time-ordered, so it doubles as the tiebreaker when history pages by
    /// <c>(occurred_on, id)</c> (sad.md §11).
    /// </remarks>
    public Guid Id { get; private set; }

    /// <summary>The one account the money moved on.</summary>
    public Guid AccountId { get; private set; }

    /// <summary>
    /// The one category the entry is filed under. Its <see cref="Category.Kind"/> is what gives
    /// this entry its direction (<c>data-model.md</c> decision 3).
    /// </summary>
    public Guid CategoryId { get; private set; }

    /// <summary>How much moved. Always above zero.</summary>
    public Money Amount { get; private set; }

    /// <summary>
    /// The calendar day the owner files the entry under. A day with no time and no timezone
    /// (<c>data-model.md</c> decision 1), so a late-evening purchase can never slide into the next
    /// month.
    /// </summary>
    public DateOnly OccurredOn { get; private set; }

    /// <summary>
    /// What the owner wrote about the entry, or <c>null</c> when they wrote nothing (AC-01).
    /// </summary>
    /// <remarks>
    /// Stored and returned as plain text, never interpreted (PRD §6.1). The entity checks the
    /// length and changes nothing else about it.
    /// </remarks>
    public string? Note { get; private set; }

    /// <summary>
    /// The reference of the outside record this entry came from. <b>Always <c>null</c> in this
    /// feature</b> — nothing here sets it (ADR-0006).
    /// </summary>
    /// <remarks>
    /// It ships unused on purpose: csv-import and message capture fill it later, and adding the
    /// column now means those features need no migration against a table that by then holds real
    /// history. PostgreSQL treats nulls as distinct in a unique index, so the unique
    /// <c>external_ref</c> permits any number of null rows — which is every row this feature
    /// writes.
    /// </remarks>
    public string? ExternalRef { get; private set; }

    /// <summary>When the entry was recorded, in UTC. An audit timestamp, taken from <c>IClock</c>.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Records a movement on one account, under one category (AC-01).
    /// </summary>
    /// <param name="id">A UUID v7 minted by the caller (ADR-0005).</param>
    /// <param name="accountId">The one account the money moved on.</param>
    /// <param name="categoryId">The category to file the entry under.</param>
    /// <param name="amount">How much moved. Must be above zero.</param>
    /// <param name="occurredOn">The calendar day the owner files it under.</param>
    /// <param name="note">What the owner wrote, or <c>null</c>.</param>
    /// <param name="createdAt">The current instant in UTC, from <c>IClock</c>.</param>
    /// <exception cref="DomainRuleViolationException">
    /// The amount is zero or below — <c>Transaction.AmountNotPositive</c> (AC-02). Or the note is
    /// longer than <see cref="MaxNoteLength"/> characters — <c>Transaction.NoteTooLong</c>.
    /// </exception>
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

    /// <summary>
    /// Applies the owner's correction to the entry in place (AC-21). The caller passes the entry's
    /// current value for every field it is not changing.
    /// </summary>
    /// <param name="accountId">The one account the entry sits on, old or new.</param>
    /// <param name="secondAccountId">
    /// A further account the owner named, or <c>null</c>. <c>null</c> is the only value this
    /// feature ever passes — anything else is refused (AC-24).
    /// </param>
    /// <param name="categoryId">The category the entry is filed under, old or new.</param>
    /// <param name="amount">The amount, old or new. Must be above zero.</param>
    /// <param name="occurredOn">The calendar day, old or new.</param>
    /// <param name="note">The note, old or new, or <c>null</c> to leave the entry without one.</param>
    /// <exception cref="DomainRuleViolationException">
    /// A second account was named — <c>Transaction.KindChangeNotAllowed</c> (AC-24). Or the amount
    /// is zero or below — <c>Transaction.AmountNotPositive</c> (AC-02). Or the note is longer than
    /// <see cref="MaxNoteLength"/> characters — <c>Transaction.NoteTooLong</c>.
    /// </exception>
    /// <remarks>
    /// <b>There is no reversing entry</b> (ADR-0003). The row itself changes, and every balance the
    /// entry affects follows on the next read, because no balance is stored (ADR-0002).
    /// <para>
    /// <b>Every check runs before the first field is assigned</b>, so a refused edit leaves the
    /// entry exactly as it was — which is what sad.md §6 flows 4 and 9 promise the owner.
    /// </para>
    /// <para>
    /// <b>The second account is refused here, not only by the request shape.</b> Presentation also
    /// refuses it structurally, because <c>EditTransactionRequest</c> sets
    /// <c>additionalProperties: false</c>. Keeping the rule in the entity as well means no surface
    /// can turn a transaction into a transfer by going around that shape.
    /// </para>
    /// </remarks>
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
