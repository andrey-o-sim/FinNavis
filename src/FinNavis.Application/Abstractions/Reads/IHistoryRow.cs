using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Abstractions.Reads;

/// <summary>
/// What every row of history carries, whichever kind of movement it is. The part the page orders
/// and pages by.
/// </summary>
/// <remarks>
/// <b>Not a port and not an entity.</b> It is the shared shape of two read rows,
/// <see cref="HistoryTransactionRow"/> and <see cref="HistoryTransferRow"/>, so one ordered list
/// can hold both — which is what a <c>UNION ALL</c> over two tables returns (ADR-0001).
/// <para>
/// <b>The kind is the type, not a field.</b> A caller switches on which record it got, so a
/// transaction can never carry a destination account and a transfer can never carry a category:
/// neither field exists on the other row. AC-16 marks each row as one or the other, and this is how
/// that mark is spelled in C#.
/// </para>
/// </remarks>
public interface IHistoryRow
{
    /// <summary>The identity of the movement. A time-ordered UUID v7 (ADR-0005).</summary>
    Guid Id { get; }

    /// <summary>
    /// The calendar day the movement is filed under. The leading part of the page order, with
    /// <see cref="Id"/> as the tiebreaker inside one day.
    /// </summary>
    DateOnly OccurredOn { get; }

    /// <summary>How much moved. Always above zero — direction is not carried by the amount.</summary>
    Money Amount { get; }

    /// <summary>When the movement was recorded, in UTC.</summary>
    DateTimeOffset CreatedAt { get; }
}
