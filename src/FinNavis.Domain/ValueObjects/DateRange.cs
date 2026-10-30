namespace FinNavis.Domain.ValueObjects;

public readonly record struct DateRange
{
    private DateRange(DateOnly from, DateOnly to)
    {
        From = from;
        To = to;
    }

    public DateOnly From { get; }

    public DateOnly To { get; }

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
}
