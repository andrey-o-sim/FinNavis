namespace FinNavis.Domain.Abstractions;

/// <summary>
/// The machine clock, behind a port, so a date rule can be tested without waiting for a date
/// (sad.md §8).
/// </summary>
public interface IClock
{
    /// <summary>
    /// The current instant, in UTC. This is what an audit timestamp such as <c>created_at</c>
    /// records.
    /// </summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>
    /// Today as a calendar day, with no time and no timezone. This is the type of the day an
    /// entry is filed under, which is a different thing from <see cref="UtcNow"/> on purpose
    /// (data-model.md decision 1).
    /// </summary>
    DateOnly Today { get; }
}
