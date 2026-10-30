using FinNavis.Domain.Abstractions;

namespace FinNavis.Application.UnitTests.Fakes;

internal sealed class FixedClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow => utcNow;

    public DateOnly Today => DateOnly.FromDateTime(utcNow.UtcDateTime);
}
