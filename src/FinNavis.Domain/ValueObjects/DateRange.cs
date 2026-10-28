namespace FinNavis.Domain.ValueObjects;

/// <summary>
/// A closed range of calendar days. Both ends are inclusive.
/// </summary>
/// <remarks>
/// Built on <c>DateOnly</c>, not on a timestamp. The day an entry is filed under has no time and
/// no timezone (data-model.md decision 1), so a period filter is a plain range of days and a
/// late-evening entry cannot slide across a period boundary.
/// </remarks>
public readonly record struct DateRange
{
    private DateRange(DateOnly from, DateOnly to)
    {
        From = from;
        To = to;
    }

    /// <summary>The first day of the range. Part of the range.</summary>
    public DateOnly From { get; }

    /// <summary>The last day of the range. Part of the range.</summary>
    public DateOnly To { get; }

    /// <summary>
    /// Builds a range from two days the caller already knows are in order.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="to"/> is before <paramref name="from"/>.
    /// </exception>
    public static DateRange Create(DateOnly from, DateOnly to)
    {
        if (!TryCreate(from, to, out DateRange range))
        {
            throw new ArgumentOutOfRangeException(
                nameof(to),
                to,
                "The last day of a range cannot be before its first day.");
        }

        return range;
    }

    /// <summary>
    /// Builds a range from two days that come from the outside, without throwing.
    /// </summary>
    /// <remarks>
    /// The caller turns a <c>false</c> into the refusal its own surface advertises —
    /// <c>History.InvalidFilter</c> or <c>Breakdown.InvalidPeriod</c>. The two codes differ, so
    /// this type cannot pick one, and catching an exception is the wrong shape for a check that
    /// is expected to fail on ordinary input.
    /// </remarks>
    /// <returns><c>true</c> when the two days form a range.</returns>
    public static bool TryCreate(DateOnly from, DateOnly to, out DateRange range)
    {
        if (to < from)
        {
            range = default;
            return false;
        }

        range = new DateRange(from, to);
        return true;
    }

    /// <summary>True when the day falls inside the range. Both ends count as inside.</summary>
    public bool Contains(DateOnly day) => day >= From && day <= To;
}
