namespace FinNavis.Domain.Abstractions;

/// <summary>
/// The machine clock, behind an interface so a domain rule that needs "now" stays testable.
/// </summary>
public interface IClock
{
    /// <summary>The current instant, in UTC.</summary>
    DateTimeOffset UtcNow { get; }
}
