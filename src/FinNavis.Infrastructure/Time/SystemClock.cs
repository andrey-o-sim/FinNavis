using FinNavis.Domain.Abstractions;

namespace FinNavis.Infrastructure.Time;

/// <summary>
/// The machine clock behind <see cref="IClock"/> (sad.md §8).
/// </summary>
/// <remarks>
/// Both values are read in UTC. <c>created_at</c> is an audit timestamp stored in UTC, and the day
/// an entry is filed under carries no timezone at all (<c>data-model.md</c> decision 1), so there
/// is no local time to convert from.
/// </remarks>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
