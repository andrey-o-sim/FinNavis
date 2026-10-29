using FinNavis.Domain.Abstractions;

namespace FinNavis.Application.UnitTests.Fakes;

/// <summary>
/// A clock that never moves, so a test can assert on the timestamp an entity was given.
/// </summary>
internal sealed class FixedClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow => utcNow;

    public DateOnly Today => DateOnly.FromDateTime(utcNow.UtcDateTime);
}
