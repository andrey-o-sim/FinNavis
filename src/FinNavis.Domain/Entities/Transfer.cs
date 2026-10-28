using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Domain.Entities;

/// <summary>
/// One movement of money between two of the owner's own accounts. An aggregate root.
/// </summary>
/// <remarks>
/// <b>Source and destination are always two different accounts</b> (AC-05). The rule is refused
/// here, before the row is ever built — not by a <c>CHECK</c> constraint, because the database stays
/// dumb storage (<c>data-model.md</c> decision 6, <c>.claude/rules/migrations.md</c>).
/// <para>
/// <b>One amount for both sides, carried by one entity.</b> AC-04 and AC-15 then hold by
/// construction: the source falls and the destination rises by the same figure, so the total across
/// accounts cannot change. AC-06 is stronger than prevention — a half-applied transfer is not
/// representable, because there is no second object to leave behind (<c>data-model.md</c> →
/// <c>transfers</c>, sad.md §6 flow 2).
/// </para>
/// <para>
/// <b>No category and no note.</b> A transfer carries no category (sad.md §12), which is what makes
/// AC-23 a refusal rather than a validation rule: the field it would need does not exist on this
/// type. The PRD gives a note only to transactions (AC-16).
/// </para>
/// <para>
/// <b>A transfer is never a transaction.</b> The two are separate entities with separate tables
/// (ADR-0001), and neither can become the other by editing — see AC-24 on <see cref="Transaction"/>.
/// </para>
/// <para>
/// <b>AC-25 (an unknown account) is not here.</b> The entity cannot look up an account. That check
/// is in the Application layer (sad.md §6 flow 2).
/// </para>
/// </remarks>
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

    /// <summary>
    /// The identity of the transfer. A UUID v7 minted in the Application layer (ADR-0005).
    /// </summary>
    /// <remarks>
    /// The value is time-ordered, so it doubles as the tiebreaker when history pages by
    /// <c>(occurred_on, id)</c> over both tables (sad.md §11).
    /// </remarks>
    public Guid Id { get; private set; }

    /// <summary>The account the money leaves.</summary>
    public Guid SourceAccountId { get; private set; }

    /// <summary>The account the money arrives in. Never the same as the source (AC-05).</summary>
    public Guid DestinationAccountId { get; private set; }

    /// <summary>
    /// How much moved. Always above zero, and the same figure on both sides.
    /// </summary>
    /// <remarks>
    /// A transfer needs no join to learn its direction, unlike a transaction: the direction is which
    /// of the two account columns an account sits in (<c>data-model.md</c> decision 3).
    /// </remarks>
    public Money Amount { get; private set; }

    /// <summary>
    /// The calendar day the owner files the transfer under. A day with no time and no timezone
    /// (<c>data-model.md</c> decision 1), the same rule as a transaction.
    /// </summary>
    public DateOnly OccurredOn { get; private set; }

    /// <summary>When the transfer was recorded, in UTC. An audit timestamp, taken from <c>IClock</c>.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Records a move of money from one account to another (AC-04).
    /// </summary>
    /// <param name="id">A UUID v7 minted by the caller (ADR-0005).</param>
    /// <param name="sourceAccountId">The account the money leaves.</param>
    /// <param name="destinationAccountId">The account the money arrives in.</param>
    /// <param name="amount">How much moved. Must be above zero.</param>
    /// <param name="occurredOn">The calendar day the owner files it under.</param>
    /// <param name="createdAt">The current instant in UTC, from <c>IClock</c>.</param>
    /// <exception cref="DomainRuleViolationException">
    /// The source and the destination are the same account — <c>Transfer.SameAccount</c> (AC-05).
    /// Or the amount is zero or below — <c>Transfer.AmountNotPositive</c>.
    /// </exception>
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

        return new Transfer(
            id, sourceAccountId, destinationAccountId, amount, occurredOn, createdAt);
    }

    /// <summary>
    /// Applies the owner's correction to the transfer in place (AC-21). The caller passes the
    /// transfer's current value for every field it is not changing.
    /// </summary>
    /// <param name="sourceAccountId">The account the money leaves, old or new.</param>
    /// <param name="destinationAccountId">The account the money arrives in, old or new.</param>
    /// <param name="amount">The amount, old or new. Must be above zero.</param>
    /// <param name="occurredOn">The calendar day, old or new.</param>
    /// <exception cref="DomainRuleViolationException">
    /// The source and the destination would be the same account — <c>Transfer.SameAccount</c>
    /// (AC-05). Or the amount is zero or below — <c>Transfer.AmountNotPositive</c>.
    /// </exception>
    /// <remarks>
    /// <b>There is no category parameter, and there is no way to add one</b> (AC-23). The entity
    /// holds no category field, so giving a transfer a category is unrepresentable here rather than
    /// validated. The Application layer turns an attempt coming off the wire into
    /// <c>Transfer.CategoryNotAllowed</c> (sad.md §6 flow 4).
    /// <para>
    /// <b>There is no reversing entry</b> (ADR-0003). The row itself changes, and both balances
    /// follow on the next read, because no balance is stored (ADR-0002).
    /// </para>
    /// <para>
    /// <b>Every check runs before the first field is assigned</b>, so a refused edit leaves the
    /// transfer exactly as it was — which is what sad.md §6 flow 4 promises the owner.
    /// </para>
    /// </remarks>
    public void Edit(
        Guid sourceAccountId,
        Guid destinationAccountId,
        Money amount,
        DateOnly occurredOn)
    {
        RequireTwoDifferentAccounts(sourceAccountId, destinationAccountId);
        RequirePositiveAmount(amount);

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
}
